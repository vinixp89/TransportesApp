using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class PlanoService
    {
        private readonly IAssinaturaPlanoRepository _assinaturaPlanoRepository;
        private readonly PagamentoService _pagamentoService;

        public PlanoService(IAssinaturaPlanoRepository assinaturaPlanoRepository, PagamentoService pagamentoService)
        {
            _assinaturaPlanoRepository = assinaturaPlanoRepository;
            _pagamentoService = pagamentoService;
        }

        // Não depende do repositório — é só o catálogo fixo do domínio, montado pra exibição.
        public IEnumerable<PlanoResponse> ObterCatalogo()
        {
            return PlanoAssinatura.ListarTodos()
                .Select(p => new PlanoResponse(p.Tipo, p.Nome, p.PrecoMensal, p.Beneficios));
        }

        public async Task<AssinaturaPlanoResponse?> ObterAssinaturaAtualAsync(Guid clienteId)
        {
            // No máximo uma das duas existe ao mesmo tempo (índice único cobre PendentePagamento +
            // Ativa juntos — ver AssinaturaPlanoConfiguration), então dá pra só tentar as duas em ordem.
            var assinatura = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId)
                ?? await _assinaturaPlanoRepository.ObterPendentePorClienteAsync(clienteId);

            return assinatura is null ? null : MapearParaResponse(assinatura);
        }

        // Assinar um plano pago cria a assinatura como PendentePagamento e devolve a URL de checkout do
        // Mercado Pago pra redirecionar o cliente — ela só vira Ativa de fato quando o PagamentoService
        // confirmar o pagamento (webhook ou sincronização manual, ver PagamentosController). O plano
        // Básico (grátis) é o único caso que ativa na hora, sem passar pelo gateway.
        public async Task<AssinarPlanoResponse> AssinarAsync(Guid clienteId, TipoPlano tipo, string emailPagador)
        {
            var plano = PlanoAssinatura.ObterPorTipo(tipo);

            var ativa = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);
            if (ativa is not null && ativa.Tipo == tipo)
                return new AssinarPlanoResponse(MapearParaResponse(ativa), null);

            // Já tem uma tentativa de assinatura em aberto (talvez de um clique anterior que não foi
            // até o fim) — cancela ela em vez de deixar penduradas, pra manter só uma preference "viva"
            // por vez e não ter que gerenciar reaproveitamento de checkout expirado.
            var pendente = await _assinaturaPlanoRepository.ObterPendentePorClienteAsync(clienteId);
            if (pendente is not null)
            {
                pendente.Cancelar();
                await _assinaturaPlanoRepository.AtualizarAsync(pendente);
            }

            if (ativa is not null)
            {
                // Trocar de plano cancela o atual já — se o pagamento do novo não for concluído, o
                // cliente fica sem plano até assinar de novo (mesma ideia de "trocar sempre cria um novo").
                ativa.Cancelar();
                await _assinaturaPlanoRepository.AtualizarAsync(ativa);
            }

            var nova = new AssinaturaPlano(clienteId, tipo);
            await _assinaturaPlanoRepository.AdicionarAsync(nova);

            if (plano.PrecoMensal == 0)
            {
                nova.Ativar();
                await _assinaturaPlanoRepository.AtualizarAsync(nova);
                return new AssinarPlanoResponse(MapearParaResponse(nova), null);
            }

            var pagamento = await _pagamentoService.IniciarPagamentoAsync(
                clienteId,
                TipoReferenciaPagamento.AssinaturaPlano,
                nova.Id,
                plano.PrecoMensal,
                $"Assinatura do plano {plano.Nome} — Vai na Boa",
                emailPagador);

            return new AssinarPlanoResponse(MapearParaResponse(nova), pagamento.CheckoutUrl);
        }

        // Status do benefício de corrida grátis mensal da assinatura ativa do cliente — ver
        // BeneficioPlanoResponse. Não muta nada, só lê o estado atual (os métodos de AssinaturaPlano
        // usados aqui são todos "puros" em relação ao mês corrente).
        public async Task<BeneficioPlanoResponse> ObterBeneficioAsync(Guid clienteId)
        {
            var assinatura = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);

            if (assinatura is null)
                return new BeneficioPlanoResponse(false, null, false, false, false);

            var plano = PlanoAssinatura.ObterPorTipo(assinatura.Tipo);

            if (plano.CorGratisPorMes is null)
                return new BeneficioPlanoResponse(false, null, false, false, false);

            var agora = DateTime.UtcNow;
            var liberado = assinatura.CorridaPagaLiberouBeneficioNoMes(agora);
            var jaUsado = assinatura.BeneficioJaUsadoNoMes(agora);

            return new BeneficioPlanoResponse(true, plano.CorGratisPorMes, liberado, jaUsado, liberado && !jaUsado);
        }

        public async Task<bool> CancelarAsync(Guid clienteId)
        {
            // Cancela tanto uma assinatura já ativa quanto uma pendente de pagamento — desistir de
            // pagar também é uma forma válida de "cancelar".
            var atual = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId)
                ?? await _assinaturaPlanoRepository.ObterPendentePorClienteAsync(clienteId);

            if (atual is null)
                return false;

            atual.Cancelar();
            await _assinaturaPlanoRepository.AtualizarAsync(atual);

            return true;
        }

        private static AssinaturaPlanoResponse MapearParaResponse(AssinaturaPlano assinatura)
        {
            var plano = PlanoAssinatura.ObterPorTipo(assinatura.Tipo);

            return new AssinaturaPlanoResponse(
                assinatura.Id,
                assinatura.Tipo,
                plano.Nome,
                plano.PrecoMensal,
                assinatura.DataInicio,
                assinatura.Status
            );
        }
    }
}
