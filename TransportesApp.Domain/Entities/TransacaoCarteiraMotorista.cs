using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Espelha TransacaoCarteira (do cliente): um lançamento imutável no extrato do motorista, um por
    // corrida creditada e um por saque solicitado/rejeitado.
    public class TransacaoCarteiraMotorista
    {
        public Guid Id { get; private set; }
        public Guid CarteiraMotoristaId { get; private set; }
        public TipoTransacaoCarteiraMotorista Tipo { get; private set; }
        public decimal Valor { get; private set; }
        public DateTime Data { get; private set; }
        public string Descricao { get; private set; } = string.Empty;

        protected TransacaoCarteiraMotorista() { }

        public TransacaoCarteiraMotorista(Guid carteiraMotoristaId, TipoTransacaoCarteiraMotorista tipo, decimal valor, string descricao)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor da transação precisa ser maior que zero.");

            Id = Guid.NewGuid();
            CarteiraMotoristaId = carteiraMotoristaId;
            Tipo = tipo;
            Valor = valor;
            Data = DateTime.UtcNow;
            Descricao = descricao;
        }
    }
}
