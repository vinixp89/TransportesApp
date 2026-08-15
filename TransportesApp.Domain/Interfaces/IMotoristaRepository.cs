using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IMotoristaRepository
    {
        Task<Motorista?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Motorista motorista);
        Task AtualizarAsync(Motorista motorista);
    }
}