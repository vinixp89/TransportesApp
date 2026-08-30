using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IAssinaturaMotoristaExecutivoRepository
    {
        Task<AssinaturaMotoristaExecutivo?> ObterAtivaPorMotoristaAsync(Guid motoristaId);
        Task<AssinaturaMotoristaExecutivo?> ObterPendentePorMotoristaAsync(Guid motoristaId);
        Task<AssinaturaMotoristaExecutivo?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(AssinaturaMotoristaExecutivo assinatura);
        Task AtualizarAsync(AssinaturaMotoristaExecutivo assinatura);
    }
}
