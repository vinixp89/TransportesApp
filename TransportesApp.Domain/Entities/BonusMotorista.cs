namespace TransportesApp.Domain.Entities
{
    // Registro do bônus de boas-vindas pago a um motorista (ver BonusMotoristaService): cada linha é
    // uma concessão, então contar linhas = saber quantas vagas da promoção já foram usadas. Um
    // motorista só pode receber uma vez (índice único em MotoristaId).
    public class BonusMotorista
    {
        public Guid Id { get; private set; }
        public Guid MotoristaId { get; private set; }
        public decimal Valor { get; private set; }
        public DateTime DataConcedido { get; private set; }

        protected BonusMotorista() { }

        public BonusMotorista(Guid motoristaId, decimal valor)
        {
            Id = Guid.NewGuid();
            MotoristaId = motoristaId;
            Valor = valor;
            DataConcedido = DateTime.UtcNow;
        }
    }
}
