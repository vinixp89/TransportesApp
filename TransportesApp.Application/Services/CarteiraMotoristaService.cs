using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Carteira/saldo do motorista, alimentada automaticamente pelo repasse das corridas finalizadas
    // (ver CorridaService.FinalizarAsync → CreditarPorCorridaAsync) e sacável via Pix ou transferência
    // bancária. O valor só sai da carteira na hora do pedido (pra não deixar sacar o mesmo saldo duas
    // vezes); quem efetiva o pagamento é o Admin, ao clicar Concluir — pra saques Pix, isso já dispara
    // o envio de verdade via IGatewayPagamentoSaque (Banco Inter); transferência bancária continua
    // manual (o Admin paga por fora e só registra aqui), porque exige dados que ainda não coletamos do
    // motorista (nome completo, CPF/CNPJ e ISPB do banco).
    public class CarteiraMotoristaService
    {
        public const decimal ValorMinimoSaque = 20m;

        private readonly ICarteiraMotoristaRepository _carteiraRepository;
        private readonly ITransacaoCarteiraMotoristaRepository _transacaoRepository;
        private readonly ISolicitacaoSaqueRepository _solicitacaoRepository;
        private readonly IGatewayPagamentoSaque _gatewayPagamentoSaque;

        public CarteiraMotoristaService(
            ICarteiraMotoristaRepository carteiraRepository,
            ITransacaoCarteiraMotoristaRepository transacaoRepository,
            ISolicitacaoSaqueRepository solicitacaoRepository,
            IGatewayPagamentoSaque gatewayPagamentoSaque)
        {
            _carteiraRepository = carteiraRepository;
            _transacaoRepository = transacaoRepository;
            _solicitacaoRepository = solicitacaoRepository;
            _gatewayPagamentoSaque = gatewayPagamentoSaque;
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

            // Checado aqui (e não só dentro de solicitacao.Concluir()) pra nunca reenviar um Pix de
            // uma solicitação que já foi processada antes.
            if (solicitacao.Status != StatusSolicitacaoSaque.Pendente)
                throw new InvalidOperationException("Essa solicitação já foi processada.");

            if (solicitacao.Tipo == TipoSaque.Pix)
            {
                // IdIdempotente = Id da própria solicitação: se essa chamada for repetida (retry de
                // rede, duplo clique), o Banco Inter trata como o mesmo pedido em vez de pagar de novo.
                var envio = new EnvioPixSolicitado(
                    solicitacao.Id.ToString(),
                    solicitacao.Valor,
                    "Vai na Boa - repasse motorista",
                    solicitacao.ChavePix!);

                // Deixa a exceção propagar se falhar (credenciais erradas, chave inválida etc.) — a
                // solicitação continua Pendente e o saldo continua debitado/reservado, sem marcar como
                // concluída sem o pagamento ter saído de verdade.
                await _gatewayPagamentoSaque.EnviarPixAsync(envio);
            }

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
