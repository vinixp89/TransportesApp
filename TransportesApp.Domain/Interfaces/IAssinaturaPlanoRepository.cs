using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IAssinaturaPlanoRepository
    {
        // Status == Ativa estritamente — usado onde "assinatura" precisa significar "já paga e em
        // vigor" (benefícios de corrida grátis, tela de planos, etc.). Uma assinatura PendentePagamento
        // nunca deve contar como ativa pra esses casos.
        Task<AssinaturaPlano?> ObterAtivaPorClienteAsync(Guid clienteId);

        // Status == PendentePagamento — usado só por PlanoService.AssinarAsync pra saber se o cliente já
        // tem uma tentativa de assinatura em aberto antes de criar outra.
        Task<AssinaturaPlano?> ObterPendentePorClienteAsync(Guid clienteId);

        Task<AssinaturaPlano?> ObterPorIdAsync(Guid id);
        Task<IEnumerable<AssinaturaPlano>> ListarPorClienteAsync(Guid clienteId);
        Task AdicionarAsync(AssinaturaPlano assinatura);
        Task AtualizarAsync(AssinaturaPlano assinatura);
    }
}
