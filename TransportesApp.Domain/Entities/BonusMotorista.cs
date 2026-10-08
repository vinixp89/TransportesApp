namespace TransportesApp.Domain.Entities
{
    // Bônus de boas-vindas de um motorista (ver BonusMotoristaService): a vaga é reservada no
    // cadastro (um registro por motorista, índice único em MotoristaId — contar linhas = vagas
    // usadas) e o valor já é creditado no saldo da carteira, mas só pode ser sacado depois que ele
    // finalizar a 1ª corrida (Liberado/DataLiberacao) — ver CarteiraMotoristaService.SolicitarSaqueAsync.
    public class BonusMotorista
    {
        public Guid Id { get; private set; }
        public Guid MotoristaId { get; private set; }
        public decimal Valor { get; private set; }
        public DateTime DataConcedido { get; private set; }
        public bool Liberado { get; private set; }
        public DateTime? DataLiberacao { get; private set; }

        protected BonusMotorista() { }

        public BonusMotorista(Guid motoristaId, decimal valor)
        {
            Id = Guid.NewGuid();
            MotoristaId = motoristaId;
            Valor = valor;
            DataConcedido = DateTime.UtcNow;
            Liberado = false;
        }

        public void Liberar()
        {
            Liberado = true;
            DataLiberacao = DateTime.UtcNow;
        }
    }
}
