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

        public PlanoService(IAssinaturaPlanoRepository assinaturaPlanoRepository)
        {
            _assinaturaPlanoRepository = assinaturaPlanoRepository;
        }

        // Não depende do repositório — é só o catálogo fixo do domínio, montado pra exibição.
        public IEnumerable<PlanoResponse> ObterCatalogo()
        {
            return PlanoAssinatura.ListarTodos()
                .Select(p => new PlanoResponse(p.Tipo, p.Nome, p.PrecoMensal, p.Beneficios));
        }

        public async Task<AssinaturaPlanoResponse?> ObterAssinaturaAtualAsync(Guid clienteId)
        {
            var assinatura = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);

            return assinatura is null ? null : MapearParaResponse(assinatura);
        }

        // Assinar um plano novo cancela a assinatura ativa atual (se existir) e cria uma nova —
        // não dá pra ter duas assinaturas ativas ao mesmo tempo (ver índice único em
        // AssinaturaPlanoConfiguration).
        public async Task<AssinaturaPlanoResponse> AssinarAsync(Guid clienteId, TipoPlano tipo)
        {
            var atual = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);

            if (atual is not null)
            {
                if (atual.Tipo == tipo)
                    return MapearParaResponse(atual);

                atual.Cancelar();
                await _assinaturaPlanoRepository.AtualizarAsync(atual);
            }

            var nova = new AssinaturaPlano(clienteId, tipo);
            await _assinaturaPlanoRepository.AdicionarAsync(nova);

            return MapearParaResponse(nova);
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
            var atual = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);

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
                assinatura.Ativa
            );
        }
    }
}
