using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Interfaces
{
    public interface IPushTokenRepository
    {
        Task<PushToken?> ObterPorTokenAsync(string token);
        Task AdicionarAsync(PushToken pushToken);
        Task AtualizarAsync(PushToken pushToken);
        Task RemoverAsync(string token);
        Task<IReadOnlyList<string>> ListarTokensPorPapelAsync(TipoUsuario papel);
    }
}
