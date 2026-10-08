using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Promoção de boas-vindas pro motorista: os 10 primeiros que se cadastrarem (a partir de
    // DataInicio) ganham R$ 20, com a finalização da 1ª corrida como pré-requisito. A vaga é reservada
    // no cadastro (ReservarAsync); o dinheiro só entra no saldo da carteira quando ele finaliza a
    // primeira corrida (LiberarAsync) — quem nunca roda fica com a vaga ocupada, mas não tem nada
    // pra sacar. Como o crédito só acontece uma vez (Liberado), qualquer corrida finalizada depois
    // da primeira é ignorada.
    //
    // Sem lock/transação serializável na contagem: no volume esperado, o risco de passar 1 vaga do
    // limite numa disputa simultânea é aceitável (mesma decisão de PromocaoLancamentoService).
    public class BonusMotoristaService
    {
        public const int LimiteVagas = 10;
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

        public async Task<BonusMotoristaStatusResponse> ObterStatusAsync()
        {
            var reservadas = await _bonusRepository.ContarAsync();
            var liberados = await _bonusRepository.ContarLiberadosAsync();

            return new BonusMotoristaStatusResponse(
                LimiteVagas, ValorBonus, reservadas, Math.Max(0, LimiteVagas - reservadas), liberados);
        }

        // Chamado pelo AuthController logo depois de criar o Motorista — quem chama é responsável por
        // não deixar uma falha aqui travar o cadastro (envolve em try/catch).
        public async Task ReservarAsync(Guid motoristaId)
        {
            if (DateTime.UtcNow < DataInicio)
                return;

            if (await _bonusRepository.ObterPorMotoristaIdAsync(motoristaId) is not null)
                return;

            if (await _bonusRepository.ContarAsync() >= LimiteVagas)
                return;

            await _bonusRepository.AdicionarAsync(new BonusMotorista(motoristaId, ValorBonus));

            await _notificacaoService.CriarParaMotoristaAsync(
                motoristaId,
                "Você ganhou um bônus de R$ 20!",
                "Finalize sua 1ª corrida e os R$ 20 entram no seu saldo, prontos pra sacar.",
                TipoNotificacao.Geral);
        }

        // Chamado pelo CorridaService depois de finalizar uma corrida — quem chama é responsável por
        // não deixar uma falha aqui derrubar a finalização (envolve em try/catch).
        public async Task LiberarAsync(Guid motoristaId)
        {
            var bonus = await _bonusRepository.ObterPorMotoristaIdAsync(motoristaId);

            if (bonus is null || bonus.Liberado)
                return;

            bonus.Liberar();
            await _bonusRepository.AtualizarAsync(bonus);

            await _carteiraMotoristaService.CreditarBonusAsync(
                motoristaId, bonus.Valor, "Bônus de boas-vindas — 1ª corrida finalizada");

            await _notificacaoService.CriarParaMotoristaAsync(
                motoristaId,
                "Bônus liberado!",
                "Você finalizou sua 1ª corrida: os R$ 20 de bônus já estão no seu saldo.",
                TipoNotificacao.Geral);
        }
    }
}
