using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ISolicitacaoSaqueRepository
    {
        Task<SolicitacaoSaque?> ObterPorIdAsync(Guid id);
        Task<IEnumerable<SolicitacaoSaque>> ListarPorMotoristaIdAsync(Guid motoristaId);
        Task<IEnumerable<SolicitacaoSaque>> ListarPendentesAsync();
        Task AdicionarAsync(SolicitacaoSaque solicitacao);
        Task AtualizarAsync(SolicitacaoSaque solicitacao);
    }
}
