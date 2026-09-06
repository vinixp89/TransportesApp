using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Interfaces
{
    public interface IPagamentoRepository
    {
        Task<Pagamento?> ObterPorIdAsync(Guid id);

        // Usado no cancelamento de corrida avulsa (ver PagamentoService.EstornarPorReferenciaAsync) —
        // acha o pagamento aprovado feito pra essa referência, pra saber o que estornar no gateway.
        Task<Pagamento?> ObterAprovadoPorReferenciaAsync(TipoReferenciaPagamento tipoReferencia, Guid referenciaId);

        Task AdicionarAsync(Pagamento pagamento);
        Task AtualizarAsync(Pagamento pagamento);
    }
}
