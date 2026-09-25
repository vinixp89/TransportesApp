using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Registro de "1 corrida grátis" concedida a um cliente numa campanha promocional (ver
    // CampanhaPromocional e PromocaoLancamentoService). Cada linha é uma concessão — contar linhas
    // filtrando por Campanha = saber quantas vagas daquela campanha já foram usadas (ver
    // PromocaoLancamentoRepository).
    public class PromocaoLancamento
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public CorFaixa Faixa { get; private set; }
        public CampanhaPromocional Campanha { get; private set; }
        public DateTime DataConcedida { get; private set; }

        protected PromocaoLancamento() { }

        public PromocaoLancamento(Guid clienteId, CorFaixa faixa, CampanhaPromocional campanha)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Faixa = faixa;
            Campanha = campanha;
            DataConcedida = DateTime.UtcNow;
        }
    }
}
