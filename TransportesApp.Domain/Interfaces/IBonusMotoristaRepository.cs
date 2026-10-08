using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IBonusMotoristaRepository
    {
        Task<int> ContarAsync();
        Task<int> ContarLiberadosAsync();
        Task<BonusMotorista?> ObterPorMotoristaIdAsync(Guid motoristaId);
        Task AdicionarAsync(BonusMotorista bonus);
        Task AtualizarAsync(BonusMotorista bonus);
    }
}
