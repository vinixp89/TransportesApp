using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Promoção de lançamento: os N primeiros clientes que se cadastram ganham 1 corrida grátis da
    // faixa Amarela (ver ConcederSeElegivelAsync, chamado pelo AuthController logo após o cadastro).
    // "Grátis" aqui é igual à doação de corrida (ver DoacaoService/PacoteCorridas.CriarDoacao): vira
    // um pacote de 1 corrida pronto pra usar, e o motorista que aceitar recebe a parte dele
    // normalmente — o custo (85% do preço da faixa) sai da empresa, não do cliente nem do motorista.
    //
    // Não usa lock/transação serializável pra proteger a contagem contra concorrência — no volume
    // de cadastros esperado no lançamento, o risco de passar 1-2 vagas do limite é aceitável frente
    // à complexidade de fazer isso à prova de corrida crítica.
    public class PromocaoLancamentoService
    {
        public const int LimiteVagas = 20;
        public const CorFaixa FaixaPromocional = CorFaixa.Amarela;

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
            if (await _promocaoRepository.ClienteJaRecebeuAsync(clienteId))
                return;

            var concedidas = await _promocaoRepository.ContarAsync();
            if (concedidas >= LimiteVagas)
                return;

            var promocao = new PromocaoLancamento(clienteId, FaixaPromocional);
            await _promocaoRepository.AdicionarAsync(promocao);

            var pacote = PacoteCorridas.CriarDoacao(clienteId, FaixaPromocional);
            await _pacoteCorridasRepository.AdicionarAsync(pacote);
        }

        public async Task<PromocaoLancamentoStatusResponse> ObterStatusAsync()
        {
            var concedidas = await _promocaoRepository.ContarAsync();
            return new PromocaoLancamentoStatusResponse(LimiteVagas, concedidas, Math.Max(0, LimiteVagas - concedidas));
        }
    }
}
