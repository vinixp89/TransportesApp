using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ICarteiraMotoristaRepository
    {
        Task<CarteiraMotorista?> ObterPorMotoristaIdAsync(Guid motoristaId);
        Task AdicionarAsync(CarteiraMotorista carteira);
        Task AtualizarAsync(CarteiraMotorista carteira);
    }
}
