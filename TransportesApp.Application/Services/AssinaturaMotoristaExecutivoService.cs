using Microsoft.Extensions.Configuration;
using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Assinatura da categoria Executivo do motorista — preço fixo (PrecoMensal), sem catálogo como o
    // AssinaturaPlano do cliente. Cobrança recorrente de verdade via Preapproval do Mercado Pago
    // (diferente do resto do sistema, que só faz pagamento único) — primeiro mês é de graça, a
    // cobrança automática começa no segundo (ver AssinarAsync).
    public class AssinaturaMotoristaExecutivoService
    {
        public const decimal PrecoMensal = 99.90m;
        // Meses de graça antes da primeira cobrança de verdade.
        private const int MesesDeGraca = 1;

        private readonly IAssinaturaMotoristaExecutivoRepository _assinaturaRepository;
        private readonly IMotoristaRepository _motoristaRepository;
        private readonly IGatewayPagamento _gateway;
        private readonly IConfiguration _configuration;

        public AssinaturaMotoristaExecutivoService(
            IAssinaturaMotoristaExecutivoRepository assinaturaRepository,
            IMotoristaRepository motoristaRepository,
            IGatewayPagamento gateway,
            IConfiguration configuration)
        {
            _assinaturaRepository = assinaturaRepository;
            _motoristaRepository = motoristaRepository;
            _gateway = gateway;
            _configuration = configuration;
        }

        public async Task<AssinaturaMotoristaExecutivoResponse?> ObterAtualAsync(Guid motoristaId)
        {
            var assinatura = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId)
                ?? await _assinaturaRepository.ObterPendentePorMotoristaAsync(motoristaId);

            return assinatura is null ? null : MapearParaResponse(assinatura);
        }

        // Cria a assinatura como PendentePagamento e devolve a URL pro motorista autorizar a cobrança
        // recorrente no Mercado Pago — só vira Ativa quando ele confirmar (webhook de preapproval, ver
        // PagamentosController). A primeira cobrança de verdade só acontece um mês depois (mês de
        // graça) — quem controla isso é o StartDate mandado pro gateway, não tem job nenhum aqui.
        public async Task<AssinarExecutivoResponse> AssinarAsync(Guid motoristaId, int anoVeiculo, string emailPagador)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId)
                ?? throw new InvalidOperationException("Motorista não encontrado.");

            if (DateTime.UtcNow.Year - anoVeiculo > 3)
                throw new InvalidOperationException("A categoria Executivo exige veículo com até 3 anos de fabricação.");

            motorista.DefinirAnoVeiculo(anoVeiculo);
            await _motoristaRepository.AtualizarAsync(motorista);

            var ativa = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId);
            if (ativa is not null)
                return new AssinarExecutivoResponse(MapearParaResponse(ativa), null);

            // Mesma ideia do PlanoService: uma tentativa anterior não concluída é cancelada antes de
            // criar outra, pra manter só uma assinatura "viva" por vez.
            var pendente = await _assinaturaRepository.ObterPendentePorMotoristaAsync(motoristaId);
            if (pendente is not null)
            {
                if (pendente.PreapprovalId is not null)
                    await _gateway.CancelarPreapprovalAsync(pendente.PreapprovalId);

                pendente.Cancelar();
                await _assinaturaRepository.AtualizarAsync(pendente);
            }

            var nova = new AssinaturaMotoristaExecutivo(motoristaId);
            await _assinaturaRepository.AdicionarAsync(nova);

            var urlRetornoBase = (_configuration["MercadoPago:UrlRetornoFrontend"] ?? "http://localhost:5173").TrimEnd('/');
            var urlRetorno = $"{urlRetornoBase}/pagamentos/retorno";

            PreapprovalCriado preapproval;
            try
            {
                preapproval = await _gateway.CriarPreapprovalAsync(new SolicitacaoPreapproval(
                    ExternalReference: nova.Id.ToString(),
                    Descricao: "Assinatura Executivo — Vai na Boa",
                    Valor: PrecoMensal,
                    EmailPagador: emailPagador,
                    PrimeiraCobranca: DateTime.UtcNow.AddMonths(MesesDeGraca),
                    UrlRetorno: urlRetorno
                ));
            }
            catch (InvalidOperationException)
            {
                // Sem isso, uma recusa do gateway (ex: e-mail inválido) deixava a assinatura presa em
                // PendentePagamento pra sempre — o motorista nunca mais conseguia tentar de novo.
                nova.Cancelar();
                await _assinaturaRepository.AtualizarAsync(nova);
                throw;
            }

            nova.RegistrarPreapproval(preapproval.PreapprovalId);
            await _assinaturaRepository.AtualizarAsync(nova);

            return new AssinarExecutivoResponse(MapearParaResponse(nova), preapproval.UrlCheckout);
        }

        public async Task<bool> CancelarAsync(Guid motoristaId)
        {
            var atual = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId)
                ?? await _assinaturaRepository.ObterPendentePorMotoristaAsync(motoristaId);

            if (atual is null)
                return false;

            // Sem isso, o Mercado Pago continuaria cobrando todo mês mesmo com a assinatura cancelada
            // aqui do nosso lado.
            if (atual.PreapprovalId is not null)
                await _gateway.CancelarPreapprovalAsync(atual.PreapprovalId);

            atual.Cancelar();
            await _assinaturaRepository.AtualizarAsync(atual);

            return true;
        }

        // Chamado pelo webhook de preapproval (ver PagamentosController) — sincroniza o status real
        // vindo do Mercado Pago com a nossa assinatura. Nunca confia no corpo da notificação: sempre
        // busca de volta na API do gateway antes de aplicar qualquer mudança, mesmo princípio do
        // PagamentoService.ProcessarNotificacaoAsync.
        public async Task ProcessarNotificacaoPreapprovalAsync(string preapprovalId)
        {
            var statusGateway = await _gateway.ConsultarPreapprovalAsync(preapprovalId);

            if (statusGateway.ExternalReference is null || !Guid.TryParse(statusGateway.ExternalReference, out var assinaturaId))
                return; // preapproval que não veio da nossa aplicação — ignora

            var assinatura = await _assinaturaRepository.ObterPorIdAsync(assinaturaId);
            if (assinatura is null)
                return;

            if (statusGateway.Status == "authorized" && assinatura.Status is StatusAssinatura.PendentePagamento or StatusAssinatura.PagamentoRecusado)
            {
                assinatura.Ativar();
                await _assinaturaRepository.AtualizarAsync(assinatura);
            }
            else if (statusGateway.Status == "cancelled" && assinatura.Status is StatusAssinatura.Ativa or StatusAssinatura.PendentePagamento)
            {
                assinatura.Cancelar();
                await _assinaturaRepository.AtualizarAsync(assinatura);
            }
            // "pending" é o estado inicial (já é PendentePagamento do nosso lado) e "paused" é uma
            // pausa temporária que o próprio Mercado Pago resolve sozinho (volta pra "authorized" ou
            // vai pra "cancelled" depois de tentativas) — nenhum dos dois muda nada aqui.
        }

        // Usado pelo CorridaService pra decidir se um motorista pode ver/aceitar corridas Executivo:
        // precisa ter assinatura ativa E veículo dentro do limite de idade.
        public async Task<bool> EstaElegivelAsync(Guid motoristaId)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(motoristaId);
            if (motorista is null || !motorista.VeiculoElegivelParaExecutivo())
                return false;

            var ativa = await _assinaturaRepository.ObterAtivaPorMotoristaAsync(motoristaId);
            return ativa is not null;
        }

        private static AssinaturaMotoristaExecutivoResponse MapearParaResponse(AssinaturaMotoristaExecutivo assinatura)
            => new(assinatura.Id, PrecoMensal, assinatura.DataInicio, assinatura.Status);
    }
}
