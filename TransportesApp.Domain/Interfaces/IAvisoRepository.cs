using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IAvisoRepository
    {
        Task<Aviso?> ObterAtivoAsync(DateTime agora);
        Task<IEnumerable<Aviso>> ListarAsync();
        Task<Aviso?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Aviso aviso);
        Task AtualizarAsync(Aviso aviso);
        Task RemoverAsync(Aviso aviso);
    }
}
