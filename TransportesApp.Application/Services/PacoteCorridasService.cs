using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class PacoteCorridasService
    {
        private readonly IPacoteCorridasRepository _pacoteCorridasRepository;
        private readonly IAssinaturaPlanoRepository _assinaturaPlanoRepository;
        private readonly PagamentoService _pagamentoService;

        public PacoteCorridasService(
            IPacoteCorridasRepository pacoteCorridasRepository,
            IAssinaturaPlanoRepository assinaturaPlanoRepository,
            PagamentoService pagamentoService)
        {
            _pacoteCorridasRepository = pacoteCorridasRepository;
            _assinaturaPlanoRepository = assinaturaPlanoRepository;
            _pagamentoService = pagamentoService;
        }

        // Não depende do repositório — é só a tabela de preços do domínio, montada pra exibição.
        public IEnumerable<CatalogoPacoteResponse> ObterCatalogo()
        {
            return FaixaDistancia.ListarTodas().Select(faixa => new CatalogoPacoteResponse(
                faixa.Cor,
                faixa.PrecoAvulso,
                FaixaDistancia.TamanhosPacoteDisponiveis
                    .Select(quantidade => new TamanhoPacoteResponse(quantidade, faixa.ObterPrecoPacote(quantidade)))
                    .ToList()
            ));
        }

        // Cria o pacote (ainda Pago=false) e abre o checkout do Mercado Pago (cartão/boleto) pelo
        // valor exato — o pacote só fica utilizável de verdade quando o pagamento confirmar (ver
        // PagamentoService.AplicarEfeitoColateralAsync).
        public async Task<IniciarCompraPacoteResponse> IniciarCompraAsync(CriarPacoteCorridasRequest request, Guid clienteId, string emailCliente)
        {
            var pacote = await CriarPendentePagamentoAsync(request, clienteId);

            var descricao = $"Pacote de {pacote.QuantidadeTotal} corridas — faixa {pacote.Faixa}";
            var pagamento = await _pagamentoService.IniciarPagamentoAsync(
                clienteId, TipoReferenciaPagamento.PacoteCorridas, pacote.Id, pacote.PrecoPago, descricao, emailCliente);

            return new IniciarCompraPacoteResponse(pacote.Id, pagamento.CheckoutUrl);
        }

        // Mesma ideia do IniciarCompraAsync, só que pagando via Pix direto (QR Code na hora).
        public async Task<IniciarCompraPacotePixResponse> IniciarCompraPixAsync(CriarPacoteCorridasRequest request, Guid clienteId, string emailCliente, string cpfCliente)
        {
            var pacote = await CriarPendentePagamentoAsync(request, clienteId);

            var descricao = $"Pacote de {pacote.QuantidadeTotal} corridas — faixa {pacote.Faixa}";
            var pix = await _pagamentoService.IniciarPagamentoPixAsync(
                clienteId, TipoReferenciaPagamento.PacoteCorridas, pacote.Id, pacote.PrecoPago, descricao, emailCliente, cpfCliente);

            return new IniciarCompraPacotePixResponse(pacote.Id, pix.PagamentoGatewayId, pix.QrCodeCopiaCola, pix.QrCodeBase64);
        }

        private async Task<PacoteCorridas> CriarPendentePagamentoAsync(CriarPacoteCorridasRequest request, Guid clienteId)
        {
            var percentualDesconto = await ObterPercentualDescontoAsync(clienteId);

            var pacote = new PacoteCorridas(clienteId, request.Faixa, request.Quantidade, percentualDesconto);

            await _pacoteCorridasRepository.AdicionarAsync(pacote);

            return pacote;
        }

        // Desconto do plano de assinatura ativo do cliente, se tiver (ver PlanoAssinatura.PercentualDescontoPacotes).
        // 0 pra quem não tem assinatura ativa ou cujo plano não dá desconto (ex: Básico).
        private async Task<decimal> ObterPercentualDescontoAsync(Guid clienteId)
        {
            var assinatura = await _assinaturaPlanoRepository.ObterAtivaPorClienteAsync(clienteId);

            if (assinatura is null)
                return 0m;

            return PlanoAssinatura.ObterPorTipo(assinatura.Tipo).PercentualDescontoPacotes;
        }

        public async Task<PacoteCorridasResponse?> ObterPorIdAsync(Guid id)
        {
            var pacote = await _pacoteCorridasRepository.ObterPorIdAsync(id);

            return pacote is null ? null : MapearParaResponse(pacote);
        }

        public async Task<IEnumerable<PacoteCorridasResponse>> ListarPorClienteAsync(Guid clienteId)
        {
            var pacotes = await _pacoteCorridasRepository.ListarPorClienteAsync(clienteId);

            // Pacote com Pago=false ainda está esperando confirmação do Mercado Pago — não existe
            // pro cliente até lá (ver PacoteCorridas.TemCorridaDisponivel).
            return pacotes.Where(p => p.Pago).Select(MapearParaResponse);
        }

        private static PacoteCorridasResponse MapearParaResponse(PacoteCorridas pacote)
        {
            return new PacoteCorridasResponse(
                pacote.Id,
                pacote.ClienteId,
                pacote.Faixa,
                pacote.QuantidadeTotal,
                pacote.QuantidadeUsada,
                pacote.QuantidadeRestante,
                pacote.PrecoPago,
                pacote.DataCompra
            );
        }
    }
}
