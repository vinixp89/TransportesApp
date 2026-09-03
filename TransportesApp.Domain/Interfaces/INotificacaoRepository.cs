using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface INotificacaoRepository
    {
        Task<Notificacao?> ObterPorIdAsync(Guid id);
        // Mais recentes primeiro — é assim que a caixa de entrada exibe.
        Task<IEnumerable<Notificacao>> ListarPorClienteAsync(Guid clienteId);
        Task<int> ContarNaoLidasPorClienteAsync(Guid clienteId);
        Task AdicionarAsync(Notificacao notificacao);
        Task AtualizarAsync(Notificacao notificacao);
    }
}
