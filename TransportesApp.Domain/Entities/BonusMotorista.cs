namespace TransportesApp.Domain.Entities
{
    // Bônus de boas-vindas de um motorista (ver BonusMotoristaService): a vaga é reservada no
    // cadastro (um registro por motorista, índice único em MotoristaId — contar linhas = vagas
    // usadas), mas o valor só entra no saldo da carteira quando ele finaliza a 1ª corrida
    // (Liberado/DataLiberacao). Até lá o dinheiro não existe na carteira, então não dá pra sacar.
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
