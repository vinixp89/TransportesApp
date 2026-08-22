namespace TransportesApp.Domain.Entities
{
    // Carteira digital do cliente: saldo pré-pago em reais que pode ser recarregado. Uma carteira
    // por cliente, criada sob demanda no primeiro acesso — não precisa existir desde o cadastro.
    public class Carteira
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public decimal Saldo { get; private set; }
        public DateTime DataCriacao { get; private set; }

        protected Carteira() { }

        public Carteira(Guid clienteId)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Saldo = 0;
            DataCriacao = DateTime.UtcNow;
        }

        public void Recarregar(decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor da recarga precisa ser maior que zero.");

            Saldo += valor;
        }

        // Ainda não usado no pagamento de corridas (isso fica pra quando o débito automático da
        // carteira for ligado ao fluxo de corrida avulsa) — mas já modela a regra pra quando isso
        // acontecer, seguindo o mesmo padrão de PacoteCorridas.UsarCorrida().
        public void Debitar(decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do débito precisa ser maior que zero.");

            if (valor > Saldo)
                throw new InvalidOperationException("Saldo insuficiente na carteira.");

            Saldo -= valor;
        }

        // Devolve saldo de uma corrida avulsa cancelada com direito a reembolso (ver Corrida.Cancelar e
        // CorridaService.ReverterConsumoAsync). Mesma mecânica de Recarregar, mas mantido como método
        // separado pra deixar claro no extrato/código que não é uma recarga paga via Mercado Pago.
        public void Estornar(decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do estorno precisa ser maior que zero.");

            Saldo += valor;
        }
    }
}
