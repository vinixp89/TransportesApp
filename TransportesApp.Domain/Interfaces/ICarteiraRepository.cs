using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ICarteiraRepository
    {
        Task<Carteira?> ObterPorClienteIdAsync(Guid clienteId);
        Task AdicionarAsync(Carteira carteira);
        Task AtualizarAsync(Carteira carteira);
    }
}
