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

        public CarteiraService(ICarteiraRepository carteiraRepository, ITransacaoCarteiraRepository transacaoCarteiraRepository)
        {
            _carteiraRepository = carteiraRepository;
            _transacaoCarteiraRepository = transacaoCarteiraRepository;
        }

        // Cria a carteira do cliente na primeira vez que ela é acessada — evita ter que mexer no
        // fluxo de cadastro de cliente só pra abrir uma carteira que a maioria pode nem usar logo.
        public async Task<CarteiraResponse> ObterOuCriarAsync(Guid clienteId)
        {
            var carteira = await ObterOuCriarEntidadeAsync(clienteId);
            return MapearParaResponse(carteira);
        }

        public async Task<CarteiraResponse> RecarregarAsync(Guid clienteId, decimal valor)
        {
            var carteira = await ObterOuCriarEntidadeAsync(clienteId);

            carteira.Recarregar(valor);
            await _carteiraRepository.AtualizarAsync(carteira);

            var transacao = new TransacaoCarteira(carteira.Id, TipoTransacaoCarteira.Recarga, valor, "Recarga de saldo");
            await _transacaoCarteiraRepository.AdicionarAsync(transacao);

            return MapearParaResponse(carteira);
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
