using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IAssinaturaMotoristaExecutivoRepository
    {
        Task<AssinaturaMotoristaExecutivo?> ObterAtivaPorMotoristaAsync(Guid motoristaId);
        Task<AssinaturaMotoristaExecutivo?> ObterPendentePorMotoristaAsync(Guid motoristaId);
        Task<AssinaturaMotoristaExecutivo?> ObterPorIdAsync(Guid id);
        // Fila de revisão do Admin (ver AssinaturaMotoristaExecutivoService.ListarAguardandoAprovacaoAsync)
        // — mais antiga primeiro, pra não deixar ninguém esperando fora de ordem.
        Task<IEnumerable<AssinaturaMotoristaExecutivo>> ListarAguardandoAprovacaoAsync();
        Task AdicionarAsync(AssinaturaMotoristaExecutivo assinatura);
        Task AtualizarAsync(AssinaturaMotoristaExecutivo assinatura);
    }
}
