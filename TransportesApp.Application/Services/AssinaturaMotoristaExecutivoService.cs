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

        // Cria a solicitação como AguardandoAprovacao — NÃO chama o gateway ainda. Placa/modelo/ano
        // são autodeclarados (ver Motorista.VeiculoElegivelParaExecutivo), então antes de cobrar
        // qualquer coisa o Admin confere manualmente a foto do carro/placa e aprova ou nega (ver
        // AprovarAsync/NegarAsync) — só na aprovação a cobrança de verdade é criada no Mercado Pago.
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
            // criar outra, pra manter só uma assinatura "viva" por vez. Cobre tanto uma tentativa ainda
            // em análise quanto uma já aprovada com Preapproval criado (aí cancela lá também).
            var pendente = await _assinaturaRepository.ObterPendentePorMotoristaAsync(motoristaId);
            if (pendente is not null)
            {
                if (pendente.PreapprovalId is not null)
                    await _gateway.CancelarPreapprovalAsync(pendente.PreapprovalId);

                pendente.Cancelar();
                await _assinaturaRepository.AtualizarAsync(pendente);
            }

            var nova = new AssinaturaMotoristaExecutivo(motoristaId, emailPagador);
            await _assinaturaRepository.AdicionarAsync(nova);

            return new AssinarExecutivoResponse(MapearParaResponse(nova), null);
        }

        // Fila de revisão do Admin (ver AdminExecutivoController) — junta cada solicitação com os
        // dados do veículo declarado, pra ele conferir a placa manualmente antes de decidir.
        public async Task<IEnumerable<SolicitacaoExecutivoAdminResponse>> ListarAguardandoAprovacaoAsync()
        {
            var solicitacoes = await _assinaturaRepository.ListarAguardandoAprovacaoAsync();
            var respostas = new List<SolicitacaoExecutivoAdminResponse>();

            foreach (var solicitacao in solicitacoes)
            {
                var motorista = await _motoristaRepository.ObterPorIdAsync(solicitacao.MotoristaId);
                if (motorista is null)
                    continue;

                respostas.Add(new SolicitacaoExecutivoAdminResponse(
                    solicitacao.Id,
                    motorista.Id,
                    motorista.PlacaVeiculo,
                    motorista.ModeloVeiculo,
                    motorista.AnoVeiculo,
                    motorista.FotoVeiculoUrl is not null,
                    motorista.FotoPlacaUrl is not null,
                    solicitacao.DataInicio
                ));
            }

            return respostas;
        }

        // Chamado pelo Admin depois de conferir a placa/foto manualmente — só aqui a cobrança de
        // verdade é criada no Mercado Pago (ver AssinarAsync, que só deixa a solicitação pronta).
        public async Task<AssinaturaMotoristaExecutivoResponse?> AprovarAsync(Guid assinaturaId)
        {
            var solicitacao = await _assinaturaRepository.ObterPorIdAsync(assinaturaId);
            if (solicitacao is null)
                return null;

            var urlRetornoBase = (_configuration["MercadoPago:UrlRetornoFrontend"] ?? "http://localhost:5173").TrimEnd('/');
            var urlRetorno = $"{urlRetornoBase}/pagamentos/retorno";

            var preapproval = await _gateway.CriarPreapprovalAsync(new SolicitacaoPreapproval(
                ExternalReference: solicitacao.Id.ToString(),
                Descricao: "Assinatura Executivo — Vai na Boa",
                Valor: PrecoMensal,
                EmailPagador: solicitacao.EmailPagador,
                PrimeiraCobranca: DateTime.UtcNow.AddMonths(MesesDeGraca),
                UrlRetorno: urlRetorno
            ));

            // Só muda o status DEPOIS do gateway confirmar — se der erro acima, a exceção sobe e a
            // solicitação continua AguardandoAprovacao, pronta pro Admin tentar aprovar de novo.
            solicitacao.Aprovar();
            solicitacao.RegistrarPreapproval(preapproval.PreapprovalId, preapproval.UrlCheckout);
            await _assinaturaRepository.AtualizarAsync(solicitacao);

            return MapearParaResponse(solicitacao);
        }

        public async Task<AssinaturaMotoristaExecutivoResponse?> NegarAsync(Guid assinaturaId, string motivo)
        {
            var solicitacao = await _assinaturaRepository.ObterPorIdAsync(assinaturaId);
            if (solicitacao is null)
                return null;

            solicitacao.Negar(motivo);
            await _assinaturaRepository.AtualizarAsync(solicitacao);

            return MapearParaResponse(solicitacao);
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
            => new(assinatura.Id, PrecoMensal, assinatura.DataInicio, assinatura.Status, assinatura.CheckoutUrl, assinatura.MotivoNegacao);
    }
}
