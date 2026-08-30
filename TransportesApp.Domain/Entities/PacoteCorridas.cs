using TransportesApp.Domain.Enums;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Domain.Entities
{
    // Um pacote é sempre de uma faixa específica (ex: "pacote de 5 corridas Azuis") — só pode ser
    // usado em corridas que caiam nessa mesma faixa de distância.
    public class PacoteCorridas
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public CorFaixa Faixa { get; private set; }
        public int QuantidadeTotal { get; private set; }
        public int QuantidadeUsada { get; private set; }
        public decimal PrecoPago { get; private set; }
        public DateTime DataCompra { get; private set; }

        public int QuantidadeRestante => QuantidadeTotal - QuantidadeUsada;
        public bool TemCorridaDisponivel => QuantidadeRestante > 0;

        protected PacoteCorridas() { }

        public PacoteCorridas(Guid clienteId, CorFaixa faixa, int quantidade)
        {
            if (!FaixaDistancia.TamanhosPacoteDisponiveis.Contains(quantidade))
                throw new ArgumentException(
                    $"Pacote de {quantidade} corridas não é um tamanho disponível. Tamanhos válidos: {string.Join(", ", FaixaDistancia.TamanhosPacoteDisponiveis)}.");

            var faixaDistancia = FaixaDistancia.ObterPorCor(faixa);

            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Faixa = faixa;
            QuantidadeTotal = quantidade;
            QuantidadeUsada = 0;
            // Preço fica travado no momento da compra — se o preço avulso da faixa mudar depois,
            // não afeta pacotes já comprados.
            PrecoPago = faixaDistancia.ObterPrecoPacote(quantidade);
            DataCompra = DateTime.UtcNow;
        }

        private PacoteCorridas(Guid clienteId, CorFaixa faixa, int quantidade, decimal precoPago)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Faixa = faixa;
            QuantidadeTotal = quantidade;
            QuantidadeUsada = 0;
            PrecoPago = precoPago;
            DataCompra = DateTime.UtcNow;
        }

        // Pacote de 1 corrida criado por uma doação (ver DoacaoService.DoarAsync) — sempre quantidade 1,
        // por isso não passa pelas regras de TamanhosPacoteDisponiveis (que só valem pra compra na loja).
        // O preço registrado é o avulso da faixa, já debitado da carteira de quem doou.
        public static PacoteCorridas CriarDoacao(Guid clienteId, CorFaixa faixa)
        {
            var precoAvulso = FaixaDistancia.ObterPorCor(faixa).PrecoAvulso;
            return new PacoteCorridas(clienteId, faixa, quantidade: 1, precoPago: precoAvulso);
        }

        // Consome uma corrida do pacote. Chamado quando uma corrida por pacote é criada.
        public void UsarCorrida()
        {
            if (!TemCorridaDisponivel)
                throw new InvalidOperationException("Este pacote não tem corridas disponíveis.");

            QuantidadeUsada++;
        }

        // Reverte um UsarCorrida() — chamado quando uma corrida por pacote é cancelada com direito a
        // reembolso (ver Corrida.Cancelar e CorridaService.ReverterConsumoAsync). Nunca deixa
        // QuantidadeUsada ficar negativa.
        public void DevolverCorrida()
        {
            if (QuantidadeUsada <= 0)
                throw new InvalidOperationException("Este pacote não tem corridas usadas para devolver.");

            QuantidadeUsada--;
        }
    }
}
