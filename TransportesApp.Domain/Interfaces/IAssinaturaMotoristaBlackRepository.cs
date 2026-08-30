using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IAssinaturaMotoristaBlackRepository
    {
        Task<AssinaturaMotoristaBlack?> ObterAtivaPorMotoristaAsync(Guid motoristaId);
        Task<AssinaturaMotoristaBlack?> ObterPendentePorMotoristaAsync(Guid motoristaId);
        Task<AssinaturaMotoristaBlack?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(AssinaturaMotoristaBlack assinatura);
        Task AtualizarAsync(AssinaturaMotoristaBlack assinatura);
    }
}
