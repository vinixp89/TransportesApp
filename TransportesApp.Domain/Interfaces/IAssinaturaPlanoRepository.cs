using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IAssinaturaPlanoRepository
    {
        Task<AssinaturaPlano?> ObterAtivaPorClienteAsync(Guid clienteId);
        Task<IEnumerable<AssinaturaPlano>> ListarPorClienteAsync(Guid clienteId);
        Task AdicionarAsync(AssinaturaPlano assinatura);
        Task AtualizarAsync(AssinaturaPlano assinatura);
    }
}
