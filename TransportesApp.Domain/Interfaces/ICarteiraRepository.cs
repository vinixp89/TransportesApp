using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ICarteiraRepository
    {
        Task<Carteira?> ObterPorClienteIdAsync(Guid clienteId);
        // Usado pelo PagamentoService pra achar a carteira a partir do Pagamento.ReferenciaId, quando
        // o tipo de referência é RecargaCarteira — não dá pra usar ObterPorClienteIdAsync ali porque o
        // Pagamento só guarda o Id da carteira, não o do cliente (mesmo padrão de IAssinaturaPlanoRepository).
        Task<Carteira?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Carteira carteira);
        Task AtualizarAsync(Carteira carteira);
    }
}
