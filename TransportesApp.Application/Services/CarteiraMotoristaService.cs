using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Carteira/saldo do motorista, alimentada automaticamente pelo repasse das corridas finalizadas
    // (ver CorridaService.FinalizarAsync → CreditarPorCorridaAsync) e sacável via Pix ou transferência
    // bancária. Não tem integração com gateway pra saque — o valor só sai da carteira na hora do pedido
    // (pra não deixar sacar o mesmo saldo duas vezes); quem transfere o dinheiro de verdade é o Admin,
    // manualmente, e marca a solicitação como concluída (ou rejeita, o que estorna o saldo).
    public class CarteiraMotoristaService
    {
        public const decimal ValorMinimoSaque = 20m;

        private readonly ICarteiraMotoristaRepository _carteiraRepository;
        private readonly ITransacaoCarteiraMotoristaRepository _transacaoRepository;
        private readonly ISolicitacaoSaqueRepository _solicitacaoRepository;

        public CarteiraMotoristaService(
            ICarteiraMotoristaRepository carteiraRepository,
            ITransacaoCarteiraMotoristaRepository transacaoRepository,
            ISolicitacaoSaqueRepository solicitacaoRepository)
        {
            _carteiraRepository = carteiraRepository;
            _transacaoRepository = transacaoRepository;
            _solicitacaoRepository = solicitacaoRepository;
        }

        public async Task<CarteiraMotoristaResponse> ObterOuCriarAsync(Guid motoristaId)
        {
            var carteira = await ObterOuCriarEntidadeAsync(motoristaId);
            return MapearParaResponse(carteira);
        }

        // Chamado pelo CorridaService assim que uma corrida com motorista é finalizada — credita
        // automaticamente os 85% que cabem a ele, sem nenhuma ação manual da sua parte.
        public async Task CreditarPorCorridaAsync(Guid motoristaId, decimal valor, string descricao)
        {
            var carteira = await ObterOuCriarEntidadeAsync(motoristaId);

            carteira.Creditar(valor);
            await _carteiraRepository.AtualizarAsync(carteira);

            var transacao = new TransacaoCarteiraMotorista(carteira.Id, TipoTransacaoCarteiraMotorista.CreditoCorrida, valor, descricao);
            await _transacaoRepository.AdicionarAsync(transacao);
        }

        public async Task<IEnumerable<TransacaoCarteiraMotoristaResponse>> ObterExtratoAsync(Guid motoristaId)
        {
            var carteira = await _carteiraRepository.ObterPorMotoristaIdAsync(motoristaId);

            if (carteira is null)
                return Enumerable.Empty<TransacaoCarteiraMotoristaResponse>();

            var transacoes = await _transacaoRepository.ListarPorCarteiraIdAsync(carteira.Id);

            return transacoes.Select(t => new TransacaoCarteiraMotoristaResponse(t.Id, t.Tipo, t.Valor, t.Data, t.Descricao));
        }

        public async Task<SolicitacaoSaqueResponse> SolicitarSaqueAsync(Guid motoristaId, SolicitarSaqueRequest request)
        {
            if (request.Valor < ValorMinimoSaque)
                throw new ArgumentException($"O valor mínimo pra saque é {ValorMinimoSaque:C}.");

            var carteira = await ObterOuCriarEntidadeAsync(motoristaId);

            carteira.Debitar(request.Valor);
            await _carteiraRepository.AtualizarAsync(carteira);

            var solicitacao = new SolicitacaoSaque(
                motoristaId,
                request.Valor,
                request.Tipo,
                request.ChavePix,
                request.Banco,
                request.Agencia,
                request.Conta,
                request.TipoConta);
            await _solicitacaoRepository.AdicionarAsync(solicitacao);

            var descricaoTipo = request.Tipo == TipoSaque.Pix ? "Pix" : "transferência bancária";
            var transacao = new TransacaoCarteiraMotorista(
                carteira.Id, TipoTransacaoCarteiraMotorista.DebitoSaque, request.Valor, $"Saque solicitado via {descricaoTipo}");
            await _transacaoRepository.AdicionarAsync(transacao);

            return MapearParaResponseSaque(solicitacao);
        }

        public async Task<IEnumerable<SolicitacaoSaqueResponse>> ListarMinhasSolicitacoesAsync(Guid motoristaId)
        {
            var solicitacoes = await _solicitacaoRepository.ListarPorMotoristaIdAsync(motoristaId);
            return solicitacoes.Select(MapearParaResponseSaque);
        }

        // A partir daqui, operações de Admin — processar o saque de verdade (fora do app) e refletir
        // o resultado aqui.
        public async Task<IEnumerable<SolicitacaoSaqueResponse>> ListarPendentesAsync()
        {
            var solicitacoes = await _solicitacaoRepository.ListarPendentesAsync();
            return solicitacoes.Select(MapearParaResponseSaque);
        }

        public async Task<SolicitacaoSaqueResponse?> ConcluirSaqueAsync(Guid solicitacaoId)
        {
            var solicitacao = await _solicitacaoRepository.ObterPorIdAsync(solicitacaoId);

            if (solicitacao is null)
                return null;

            solicitacao.Concluir();
            await _solicitacaoRepository.AtualizarAsync(solicitacao);

            return MapearParaResponseSaque(solicitacao);
        }

        // Rejeitar devolve o valor pro saldo do motorista — o débito no pedido foi só uma reserva.
        public async Task<SolicitacaoSaqueResponse?> RejeitarSaqueAsync(Guid solicitacaoId, string motivo)
        {
            var solicitacao = await _solicitacaoRepository.ObterPorIdAsync(solicitacaoId);

            if (solicitacao is null)
                return null;

            solicitacao.Rejeitar(motivo);
            await _solicitacaoRepository.AtualizarAsync(solicitacao);

            var carteira = await ObterOuCriarEntidadeAsync(solicitacao.MotoristaId);
            carteira.Creditar(solicitacao.Valor);
            await _carteiraRepository.AtualizarAsync(carteira);

            var transacao = new TransacaoCarteiraMotorista(
                carteira.Id, TipoTransacaoCarteiraMotorista.EstornoSaque, solicitacao.Valor, $"Saque rejeitado: {motivo}");
            await _transacaoRepository.AdicionarAsync(transacao);

            return MapearParaResponseSaque(solicitacao);
        }

        private async Task<CarteiraMotorista> ObterOuCriarEntidadeAsync(Guid motoristaId)
        {
            var carteira = await _carteiraRepository.ObterPorMotoristaIdAsync(motoristaId);

            if (carteira is null)
            {
                carteira = new CarteiraMotorista(motoristaId);
                await _carteiraRepository.AdicionarAsync(carteira);
            }

            return carteira;
        }

        private static CarteiraMotoristaResponse MapearParaResponse(CarteiraMotorista carteira)
        {
            return new CarteiraMotoristaResponse(carteira.Id, carteira.MotoristaId, carteira.Saldo, ValorMinimoSaque, carteira.DataCriacao);
        }

        private static SolicitacaoSaqueResponse MapearParaResponseSaque(SolicitacaoSaque s)
        {
            return new SolicitacaoSaqueResponse(
                s.Id, s.Valor, s.Tipo, s.ChavePix, s.Banco, s.Agencia, s.Conta, s.TipoConta,
                s.Status, s.DataSolicitacao, s.DataProcessamento, s.MotivoRejeicao);
        }
    }
}
