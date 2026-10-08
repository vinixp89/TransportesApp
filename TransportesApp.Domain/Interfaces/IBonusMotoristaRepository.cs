using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IBonusMotoristaRepository
    {
        Task<int> ContarAsync();
        Task<bool> MotoristaJaRecebeuAsync(Guid motoristaId);
        Task AdicionarAsync(BonusMotorista bonus);
    }
}
