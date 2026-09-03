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
        // Marca pacotes concedidos de graça pela empresa (promoção de lançamento — ver
        // PromocaoLancamentoService). Corrida cancelada desse pacote não devolve a corrida pro
        // cliente (ver CorridaService.ReverterConsumoAsync) e o pacote não pode ser doado (ver
        // DoacaoService) — sem isso, dava pra cancelar a corrida de brinde repetidamente ou doar
        // ela pra outra conta e converter o presente em algo que não era a intenção da promoção.
        public bool EhPromocional { get; private set; }

        public int QuantidadeRestante => QuantidadeTotal - QuantidadeUsada;
        public bool TemCorridaDisponivel => QuantidadeRestante > 0;

        protected PacoteCorridas() { }

        // percentualDesconto vem do plano de assinatura ativo do cliente, se tiver (ver
        // PlanoAssinatura.PercentualDescontoPacotes e PacoteCorridasService.CriarAsync) — 0 pra quem
        // não tem plano com desconto. Aplicado aqui, não no controller/service, pra garantir que o
        // preço registrado sempre reflita exatamente o que foi cobrado.
        public PacoteCorridas(Guid clienteId, CorFaixa faixa, int quantidade, decimal percentualDesconto = 0m)
        {
            if (!FaixaDistancia.TamanhosPacoteDisponiveis.Contains(quantidade))
                throw new ArgumentException(
                    $"Pacote de {quantidade} corridas não é um tamanho disponível. Tamanhos válidos: {string.Join(", ", FaixaDistancia.TamanhosPacoteDisponiveis)}.");

            if (percentualDesconto is < 0m or >= 1m)
                throw new ArgumentException("Percentual de desconto inválido.");

            var faixaDistancia = FaixaDistancia.ObterPorCor(faixa);

            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Faixa = faixa;
            QuantidadeTotal = quantidade;
            QuantidadeUsada = 0;
            // Preço fica travado no momento da compra — se o preço avulso da faixa ou o plano do
            // cliente mudarem depois, não afeta pacotes já comprados.
            PrecoPago = Math.Round(faixaDistancia.ObterPrecoPacote(quantidade) * (1 - percentualDesconto), 2, MidpointRounding.AwayFromZero);
            DataCompra = DateTime.UtcNow;
        }

        private PacoteCorridas(Guid clienteId, CorFaixa faixa, int quantidade, decimal precoPago, bool ehPromocional = false)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Faixa = faixa;
            QuantidadeTotal = quantidade;
            QuantidadeUsada = 0;
            PrecoPago = precoPago;
            DataCompra = DateTime.UtcNow;
            EhPromocional = ehPromocional;
        }

        // Pacote de 1 corrida criado por uma doação (ver DoacaoService.DoarAsync) — sempre quantidade 1,
        // por isso não passa pelas regras de TamanhosPacoteDisponiveis (que só valem pra compra na loja).
        // O preço registrado é o avulso da faixa, já debitado da carteira de quem doou.
        public static PacoteCorridas CriarDoacao(Guid clienteId, CorFaixa faixa)
        {
            var precoAvulso = FaixaDistancia.ObterPorCor(faixa).PrecoAvulso;
            return new PacoteCorridas(clienteId, faixa, quantidade: 1, precoPago: precoAvulso);
        }

        // Pacote de 1 corrida concedido de graça pela empresa (promoção de lançamento — ver
        // PromocaoLancamentoService). PrecoPago fica 0 porque o cliente não pagou nada — o custo é
        // absorvido pela empresa na hora em que a corrida é aceita por um motorista.
        public static PacoteCorridas CriarPromocional(Guid clienteId, CorFaixa faixa)
        {
            return new PacoteCorridas(clienteId, faixa, quantidade: 1, precoPago: 0m, ehPromocional: true);
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
