using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ICorridaRepository
    {
        Task<Corrida?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Corrida corrida);
        Task AtualizarAsync(Corrida corrida);
    }
}