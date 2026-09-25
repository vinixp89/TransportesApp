using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Interfaces
{
    public interface IPromocaoLancamentoRepository
    {
        Task<int> ContarAsync(CampanhaPromocional campanha);
        Task<bool> ClienteJaRecebeuAsync(Guid clienteId, CampanhaPromocional campanha);
        Task AdicionarAsync(PromocaoLancamento promocao);
    }
}
