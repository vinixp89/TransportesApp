using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IPacoteCorridasRepository
    {
        Task<PacoteCorridas?> ObterPorIdAsync(Guid id);
        Task<IEnumerable<PacoteCorridas>> ListarPorClienteAsync(Guid clienteId);
        Task AdicionarAsync(PacoteCorridas pacote);
        Task AtualizarAsync(PacoteCorridas pacote);
    }
}
