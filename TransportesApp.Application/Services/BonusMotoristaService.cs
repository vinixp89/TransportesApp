using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Promoção de boas-vindas pro motorista: os 10 primeiros que se cadastrarem (a partir de
    // DataInicio) ganham R$ 20, com a finalização da 1ª corrida como pré-requisito pra sacar. O valor
    // entra no saldo da carteira já no cadastro (ReservarAsync), mas travado: CarteiraMotoristaService
    // só deixa sacar o que passa da parte bloqueada. Quando ele finaliza a 1ª corrida, LiberarAsync
    // destrava o bônus — quem nunca roda fica com o valor no saldo, sem poder sacar. Liberar só tem
    // efeito uma vez, então as corridas seguintes são ignoradas.
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
        private readonly IMotoristaRepository _motoristaRepository;

        public BonusMotoristaService(
            IBonusMotoristaRepository bonusRepository,
            CarteiraMotoristaService carteiraMotoristaService,
            NotificacaoService notificacaoService,
            IMotoristaRepository motoristaRepository)
        {
            _bonusRepository = bonusRepository;
            _carteiraMotoristaService = carteiraMotoristaService;
            _notificacaoService = notificacaoService;
            _motoristaRepository = motoristaRepository;
        }

        public async Task<BonusMotoristaStatusResponse> ObterStatusAsync()
        {
            var reservadas = await _bonusRepository.ContarAsync();
            var liberados = await _bonusRepository.ContarLiberadosAsync();

            return new BonusMotoristaStatusResponse(
                LimiteVagas, ValorBonus, reservadas, Math.Max(0, LimiteVagas - reservadas), liberados);
        }

        // Um por vez (sem Task.WhenAll): o DbContext é compartilhado na requisição e o EF Core não
        // aceita duas consultas concorrentes nele. No máximo LimiteVagas linhas, então N+1 é irrelevante.
        public async Task<IEnumerable<BonusMotoristaItemResponse>> ListarAsync()
        {
            var bonus = await _bonusRepository.ListarAsync();
            var resultado = new List<BonusMotoristaItemResponse>();

            foreach (var b in bonus)
            {
                var motorista = await _motoristaRepository.ObterPorIdAsync(b.MotoristaId);

                resultado.Add(new BonusMotoristaItemResponse(
                    b.MotoristaId, motorista?.Nome ?? "—", motorista?.Telefone ?? "—",
                    b.Valor, b.DataConcedido, b.Liberado, b.DataLiberacao));
            }

            return resultado;
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

            await _carteiraMotoristaService.CreditarBonusAsync(
                motoristaId, ValorBonus, "Bônus de boas-vindas (libera pra saque na 1ª corrida finalizada)");

            await _notificacaoService.CriarParaMotoristaAsync(
                motoristaId,
                "Você ganhou um bônus de R$ 20!",
                "Os R$ 20 já estão no seu saldo. Finalize sua 1ª corrida pra liberar o saque.",
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

            await _notificacaoService.CriarParaMotoristaAsync(
                motoristaId,
                "Bônus liberado!",
                "Você finalizou sua 1ª corrida: os R$ 20 de bônus já estão liberados pra saque.",
                TipoNotificacao.Geral);
        }
    }
}
