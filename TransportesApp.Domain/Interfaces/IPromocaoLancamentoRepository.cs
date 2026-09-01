using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IPromocaoLancamentoRepository
    {
        Task<int> ContarAsync();
        Task<bool> ClienteJaRecebeuAsync(Guid clienteId);
        Task AdicionarAsync(PromocaoLancamento promocao);
    }
}
