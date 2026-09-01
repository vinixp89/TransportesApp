using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Registro de "1 corrida grátis" concedida a um cliente na promoção de lançamento (as N
    // primeiras contas cadastradas). Cada linha é uma concessão — contar linhas = saber quantas
    // vagas já foram usadas (ver PromocaoLancamentoService).
    public class PromocaoLancamento
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public CorFaixa Faixa { get; private set; }
        public DateTime DataConcedida { get; private set; }

        protected PromocaoLancamento() { }

        public PromocaoLancamento(Guid clienteId, CorFaixa faixa)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Faixa = faixa;
            DataConcedida = DateTime.UtcNow;
        }
    }
}
