using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    public class CarteiraService
    {
        private readonly ICarteiraRepository _carteiraRepository;
        private readonly ITransacaoCarteiraRepository _transacaoCarteiraRepository;
        private readonly PagamentoService _pagamentoService;

        public CarteiraService(
            ICarteiraRepository carteiraRepository,
            ITransacaoCarteiraRepository transacaoCarteiraRepository,
            PagamentoService pagamentoService)
        {
            _carteiraRepository = carteiraRepository;
            _transacaoCarteiraRepository = transacaoCarteiraRepository;
            _pagamentoService = pagamentoService;
        }

        // Cria a carteira do cliente na primeira vez que ela é acessada — evita ter que mexer no
        // fluxo de cadastro de cliente só pra abrir uma carteira que a maioria pode nem usar logo.
        public async Task<CarteiraResponse> ObterOuCriarAsync(Guid clienteId)
        {
            var carteira = await ObterOuCriarEntidadeAsync(clienteId);
            return MapearParaResponse(carteira);
        }

        // Credita o saldo direto, sem passar por pagamento nenhum — não é mais chamado pelo endpoint
        // público de recarga (ver IniciarRecargaAsync), fica só como operação de baixo nível pra uso
        // interno/administrativo futuro. Quem quiser recarregar de verdade usa IniciarRecargaAsync.
        public async Task<CarteiraResponse> RecarregarAsync(Guid clienteId, decimal valor)
        {
            var carteira = await ObterOuCriarEntidadeAsync(clienteId);

            carteira.Recarregar(valor);
            await _carteiraRepository.AtualizarAsync(carteira);

            var transacao = new TransacaoCarteira(carteira.Id, TipoTransacaoCarteira.Recarga, valor, "Recarga de saldo");
            await _transacaoCarteiraRepository.AdicionarAsync(transacao);

            return MapearParaResponse(carteira);
        }

        // Ponto de entrada real da recarga: cria a carteira se ainda não existir e inicia um pagamento
        // genérico (ver PagamentoService) referenciando o Id da carteira. O saldo só é creditado quando
        // o Mercado Pago confirmar o pagamento (ver PagamentoService.AplicarEfeitoColateralAsync) — por
        // isso devolve só a URL de checkout, sem CarteiraResponse nenhum ainda.
        public async Task<IniciarRecargaResponse> IniciarRecargaAsync(Guid clienteId, decimal valor, string emailPagador)
        {
            var carteira = await ObterOuCriarEntidadeAsync(clienteId);

            var pagamento = await _pagamentoService.IniciarPagamentoAsync(
                clienteId,
                TipoReferenciaPagamento.RecargaCarteira,
                carteira.Id,
                valor,
                "Recarga de carteira — Vai na Boa",
                emailPagador);

            return new IniciarRecargaResponse(pagamento.CheckoutUrl);
        }

        public async Task<IEnumerable<TransacaoCarteiraResponse>> ObterExtratoAsync(Guid clienteId)
        {
            var carteira = await _carteiraRepository.ObterPorClienteIdAsync(clienteId);

            if (carteira is null)
                return Enumerable.Empty<TransacaoCarteiraResponse>();

            var transacoes = await _transacaoCarteiraRepository.ListarPorCarteiraIdAsync(carteira.Id);

            return transacoes.Select(t => new TransacaoCarteiraResponse(t.Id, t.Tipo, t.Valor, t.Data, t.Descricao));
        }

        private async Task<Carteira> ObterOuCriarEntidadeAsync(Guid clienteId)
        {
            var carteira = await _carteiraRepository.ObterPorClienteIdAsync(clienteId);

            if (carteira is null)
            {
                carteira = new Carteira(clienteId);
                await _carteiraRepository.AdicionarAsync(carteira);
            }

            return carteira;
        }

        private static CarteiraResponse MapearParaResponse(Carteira carteira)
        {
            return new CarteiraResponse(carteira.Id, carteira.ClienteId, carteira.Saldo, carteira.DataCriacao);
        }
    }
}
