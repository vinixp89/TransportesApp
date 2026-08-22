using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IPagamentoRepository
    {
        Task<Pagamento?> ObterPorIdAsync(Guid id);

        Task AdicionarAsync(Pagamento pagamento);
        Task AtualizarAsync(Pagamento pagamento);
    }
}
