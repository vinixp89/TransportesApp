using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Assinatura de um motorista à categoria Executivo (ver AssinaturaMotoristaExecutivoService) — preço fixo
    // (AssinaturaMotoristaExecutivoService.PrecoMensal), sem catálogo de planos como o AssinaturaPlano do
    // cliente. Um motorista tem no máximo UMA assinatura "em aberto" por vez (pendente de pagamento
    // OU ativa) — mesmo índice único e mesmo ciclo de vida do AssinaturaPlano (ver
    // AssinaturaMotoristaExecutivoConfiguration).
    //
    // Cobrança é um pagamento único (Checkout Pro), não recorrência de verdade — mesma limitação já
    // aceita no AssinaturaPlano hoje: fica Ativa indefinidamente até o motorista cancelar, sem
    // recobrança automática mensal.
    public class AssinaturaMotoristaExecutivo
    {
        public Guid Id { get; private set; }
        public Guid MotoristaId { get; private set; }
        public DateTime DataInicio { get; private set; }
        public DateTime? DataCancelamento { get; private set; }
        public StatusAssinatura Status { get; private set; }

        protected AssinaturaMotoristaExecutivo() { }

        public AssinaturaMotoristaExecutivo(Guid motoristaId)
        {
            Id = Guid.NewGuid();
            MotoristaId = motoristaId;
            DataInicio = DateTime.UtcNow;
            Status = StatusAssinatura.PendentePagamento;
        }

        // Chamado pelo PagamentoService quando o pagamento associado é aprovado.
        public void Ativar()
        {
            if (Status is not (StatusAssinatura.PendentePagamento or StatusAssinatura.PagamentoRecusado))
                throw new InvalidOperationException($"Não é possível ativar uma assinatura com status {Status}.");

            Status = StatusAssinatura.Ativa;
        }

        // Chamado pelo PagamentoService quando o pagamento associado é recusado/cancelado.
        public void MarcarPagamentoRecusado()
        {
            if (Status != StatusAssinatura.PendentePagamento)
                throw new InvalidOperationException($"Não é possível recusar uma assinatura com status {Status}.");

            Status = StatusAssinatura.PagamentoRecusado;
        }

        public void Cancelar()
        {
            if (Status is not (StatusAssinatura.Ativa or StatusAssinatura.PendentePagamento))
                throw new InvalidOperationException($"Essa assinatura não pode ser cancelada (status atual: {Status}).");

            Status = StatusAssinatura.Cancelada;
            DataCancelamento = DateTime.UtcNow;
        }
    }
}
