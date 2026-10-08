using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Promoção de boas-vindas pro motorista: os 5 primeiros motoristas (cadastrados a partir de
    // DataInicio) que finalizam a 1ª corrida ganham R$ 20 no saldo da carteira, sacável como qualquer
    // outro valor. A vaga só é consumida na hora da 1ª corrida finalizada — cadastro sem corrida não
    // ocupa vaga. Como só vale pra quem se cadastrou depois de DataInicio, a primeira finalização
    // dessa pessoa é necessariamente a 1ª corrida dela; o índice único em MotoristaId garante o
    // pagamento uma vez só.
    //
    // Sem lock/transação serializável na contagem: no volume esperado, o risco de passar 1 vaga do
    // limite numa disputa simultânea é aceitável (mesma decisão de PromocaoLancamentoService).
    public class BonusMotoristaService
    {
        public const int LimiteVagas = 5;
        public const decimal ValorBonus = 20m;
        public static readonly DateTime DataInicio = new(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);

        private readonly IBonusMotoristaRepository _bonusRepository;
        private readonly CarteiraMotoristaService _carteiraMotoristaService;
        private readonly NotificacaoService _notificacaoService;

        public BonusMotoristaService(
            IBonusMotoristaRepository bonusRepository,
            CarteiraMotoristaService carteiraMotoristaService,
            NotificacaoService notificacaoService)
        {
            _bonusRepository = bonusRepository;
            _carteiraMotoristaService = carteiraMotoristaService;
            _notificacaoService = notificacaoService;
        }

        // Chamado pelo CorridaService depois de finalizar uma corrida — quem chama é responsável por
        // não deixar uma falha aqui derrubar a finalização (envolve em try/catch).
        public async Task ConcederSeElegivelAsync(Motorista motorista)
        {
            if (motorista.DataCadastro < DataInicio)
                return;

            if (await _bonusRepository.MotoristaJaRecebeuAsync(motorista.Id))
                return;

            if (await _bonusRepository.ContarAsync() >= LimiteVagas)
                return;

            await _bonusRepository.AdicionarAsync(new BonusMotorista(motorista.Id, ValorBonus));

            await _carteiraMotoristaService.CreditarBonusAsync(
                motorista.Id, ValorBonus, "Bônus de boas-vindas — 1ª corrida finalizada");

            await _notificacaoService.CriarParaMotoristaAsync(
                motorista.Id,
                "Você ganhou R$ 20!",
                "Bônus de boas-vindas por finalizar sua 1ª corrida. O valor já está no seu saldo.",
                TipoNotificacao.Geral);
        }
    }
}
