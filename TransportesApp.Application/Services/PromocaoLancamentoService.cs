using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Promoções de cadastro: os N primeiros clientes que se cadastram (dentro do período de cada
    // campanha) ganham 1 corrida grátis de uma faixa fixa (ver ConcederSeElegivelAsync e
    // ConcederOutubroSeElegivelAsync, chamados pelo AuthController logo após o cadastro). "Grátis"
    // aqui é igual à doação de corrida (ver DoacaoService/PacoteCorridas.CriarDoacao): vira um
    // pacote de 1 corrida pronto pra usar, e o motorista que aceitar recebe a parte dele
    // normalmente — o custo (85% do preço da faixa) sai da empresa, não do cliente nem do motorista.
    //
    // Não usa lock/transação serializável pra proteger a contagem contra concorrência — no volume
    // de cadastros esperado, o risco de passar 1-2 vagas do limite é aceitável frente à
    // complexidade de fazer isso à prova de corrida crítica.
    public class PromocaoLancamentoService
    {
        public const int LimiteVagas = 10;
        public const CorFaixa FaixaPromocional = CorFaixa.Amarela;

        // Campanha de 01/10/2026: só concede pra cadastros feitos a partir dessa data (quem já se
        // cadastrou antes não recebe — ver ConcederOutubroSeElegivelAsync). Data em UTC porque é
        // assim que DataCadastro/DateTime.UtcNow são gravados em todo o resto do sistema.
        public const int LimiteVagasOutubro = 100;
        public const CorFaixa FaixaPromocionalOutubro = CorFaixa.Azul;
        public static readonly DateTime DataInicioOutubro = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly IPromocaoLancamentoRepository _promocaoRepository;
        private readonly IPacoteCorridasRepository _pacoteCorridasRepository;

        public PromocaoLancamentoService(
            IPromocaoLancamentoRepository promocaoRepository,
            IPacoteCorridasRepository pacoteCorridasRepository)
        {
            _promocaoRepository = promocaoRepository;
            _pacoteCorridasRepository = pacoteCorridasRepository;
        }

        // Chamado pelo AuthController logo depois de criar o Cliente — nunca deve travar o cadastro
        // em si, então quem chama isso é responsável por envolver num try/catch (mesmo padrão já
        // usado pro e-mail de boas-vindas).
        public async Task ConcederSeElegivelAsync(Guid clienteId)
        {
            await ConcederAsync(clienteId, CampanhaPromocional.Lancamento, FaixaPromocional, LimiteVagas);
        }

        // Igual à de cima, mas só concede a partir de 01/10/2026 (ver DataInicioOutubro) — quem se
        // cadastrou antes disso nunca fica elegível, mesmo que ainda sobrem vagas depois.
        public async Task ConcederOutubroSeElegivelAsync(Guid clienteId)
        {
            if (DateTime.UtcNow < DataInicioOutubro)
                return;

            await ConcederAsync(clienteId, CampanhaPromocional.Faixa100Outubro2026, FaixaPromocionalOutubro, LimiteVagasOutubro);
        }

        private async Task ConcederAsync(Guid clienteId, CampanhaPromocional campanha, CorFaixa faixa, int limiteVagas)
        {
            if (await _promocaoRepository.ClienteJaRecebeuAsync(clienteId, campanha))
                return;

            var concedidas = await _promocaoRepository.ContarAsync(campanha);
            if (concedidas >= limiteVagas)
                return;

            var promocao = new PromocaoLancamento(clienteId, faixa, campanha);
            await _promocaoRepository.AdicionarAsync(promocao);

            var pacote = PacoteCorridas.CriarPromocional(clienteId, faixa);
            await _pacoteCorridasRepository.AdicionarAsync(pacote);
        }

        public async Task<PromocaoLancamentoStatusResponse> ObterStatusAsync()
        {
            var concedidas = await _promocaoRepository.ContarAsync(CampanhaPromocional.Lancamento);
            return new PromocaoLancamentoStatusResponse(LimiteVagas, concedidas, Math.Max(0, LimiteVagas - concedidas));
        }

        public async Task<PromocaoOutubroStatusResponse> ObterStatusOutubroAsync()
        {
            var concedidas = await _promocaoRepository.ContarAsync(CampanhaPromocional.Faixa100Outubro2026);
            var vagasRestantes = Math.Max(0, LimiteVagasOutubro - concedidas);
            var ativa = DateTime.UtcNow >= DataInicioOutubro && vagasRestantes > 0;

            return new PromocaoOutubroStatusResponse(LimiteVagasOutubro, concedidas, vagasRestantes, ativa, DataInicioOutubro);
        }
    }
}
