using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Um lançamento no extrato da carteira — cada recarga (ou, no futuro, débito) gera um registro
    // aqui, preservado mesmo que o saldo da carteira mude depois (histórico imutável).
    public class TransacaoCarteira
    {
        public Guid Id { get; private set; }
        public Guid CarteiraId { get; private set; }
        public TipoTransacaoCarteira Tipo { get; private set; }
        public decimal Valor { get; private set; }
        public DateTime Data { get; private set; }
        public string Descricao { get; private set; } = string.Empty;

        protected TransacaoCarteira() { }

        public TransacaoCarteira(Guid carteiraId, TipoTransacaoCarteira tipo, decimal valor, string descricao)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor da transação precisa ser maior que zero.");

            Id = Guid.NewGuid();
            CarteiraId = carteiraId;
            Tipo = tipo;
            Valor = valor;
            Data = DateTime.UtcNow;
            Descricao = descricao;
        }
    }
}
