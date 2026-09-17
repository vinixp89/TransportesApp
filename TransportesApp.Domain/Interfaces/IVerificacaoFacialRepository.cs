using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IVerificacaoFacialRepository
    {
        Task AdicionarAsync(VerificacaoFacial verificacao);
        Task AtualizarAsync(VerificacaoFacial verificacao);
        Task<VerificacaoFacial?> ObterPorIdAsync(Guid id);
        Task<IEnumerable<VerificacaoFacial>> ListarPorCorridaAsync(Guid corridaId);

        // Mais recentes primeiro, pro painel de revisão do Admin.
        Task<IEnumerable<VerificacaoFacial>> ListarRecentesAsync(int quantidade);
    }
}
