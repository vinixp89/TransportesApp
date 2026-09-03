using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ITransacaoCarteiraMotoristaRepository
    {
        Task AdicionarAsync(TransacaoCarteiraMotorista transacao);
        Task<IEnumerable<TransacaoCarteiraMotorista>> ListarPorCarteiraIdAsync(Guid carteiraMotoristaId);
    }
}
