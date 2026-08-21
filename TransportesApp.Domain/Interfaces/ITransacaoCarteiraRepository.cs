using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ITransacaoCarteiraRepository
    {
        Task AdicionarAsync(TransacaoCarteira transacao);
        Task<IEnumerable<TransacaoCarteira>> ListarPorCarteiraIdAsync(Guid carteiraId);
    }
}
