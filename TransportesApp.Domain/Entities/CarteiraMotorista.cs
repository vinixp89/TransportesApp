namespace TransportesApp.Domain.Entities
{
    // Espelha Carteira (do cliente), mas do lado do motorista: saldo acumulado com o repasse das
    // corridas finalizadas (85% do valor da faixa — ver CorridaService.FinalizarAsync), que ele pode
    // sacar via Pix ou transferência (ver SolicitacaoSaque). Uma carteira por motorista, criada sob
    // demanda na primeira corrida finalizada ou no primeiro acesso à tela de saldo.
    public class CarteiraMotorista
    {
        public Guid Id { get; private set; }
        public Guid MotoristaId { get; private set; }
        public decimal Saldo { get; private set; }
        public DateTime DataCriacao { get; private set; }

        protected CarteiraMotorista() { }

        public CarteiraMotorista(Guid motoristaId)
        {
            Id = Guid.NewGuid();
            MotoristaId = motoristaId;
            Saldo = 0;
            DataCriacao = DateTime.UtcNow;
        }

        public void Creditar(decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do crédito precisa ser maior que zero.");

            Saldo += valor;
        }

        public void Debitar(decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do débito precisa ser maior que zero.");

            if (valor > Saldo)
                throw new InvalidOperationException("Saldo insuficiente pra esse saque.");

            Saldo -= valor;
        }
    }
}
