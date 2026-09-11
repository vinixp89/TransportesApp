using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Assinatura de um motorista à categoria Executivo (ver AssinaturaMotoristaExecutivoService) — preço fixo
    // (AssinaturaMotoristaExecutivoService.PrecoMensal), sem catálogo de planos como o AssinaturaPlano do
    // cliente. Um motorista tem no máximo UMA assinatura "em aberto" por vez (pendente de pagamento
    // OU ativa) — mesmo índice único e mesmo ciclo de vida do AssinaturaPlano (ver
    // AssinaturaMotoristaExecutivoConfiguration).
    //
    // Cobrança recorrente de verdade via Preapproval do Mercado Pago (não Checkout Pro de pagamento
    // único como o resto do sistema) — ver MercadoPagoGateway.CriarPreapprovalAsync. Primeira cobrança
    // sai um mês depois da assinatura (mês de graça); dali em diante, o próprio Mercado Pago cobra
    // automaticamente todo mês, sem job nenhum do nosso lado — a gente só reage ao webhook de
    // preapproval (autorização/cancelamento), ver PagamentosController.
    public class AssinaturaMotoristaExecutivo
    {
        public Guid Id { get; private set; }
        public Guid MotoristaId { get; private set; }
        public DateTime DataInicio { get; private set; }
        public DateTime? DataCancelamento { get; private set; }
        public StatusAssinatura Status { get; private set; }
        // Id da assinatura recorrente no Mercado Pago — usado pra cancelar lá quando o motorista
        // cancela aqui (ver AssinaturaMotoristaExecutivoService.CancelarAsync).
        public string? PreapprovalId { get; private set; }

        protected AssinaturaMotoristaExecutivo() { }

        public AssinaturaMotoristaExecutivo(Guid motoristaId)
        {
            Id = Guid.NewGuid();
            MotoristaId = motoristaId;
            DataInicio = DateTime.UtcNow;
            Status = StatusAssinatura.PendentePagamento;
        }

        // Chamado assim que o Preapproval é criado no Mercado Pago (ver
        // AssinaturaMotoristaExecutivoService.AssinarAsync) — antes ainda do motorista autorizar.
        public void RegistrarPreapproval(string preapprovalId) => PreapprovalId = preapprovalId;

        // Chamado quando o Mercado Pago confirma que o motorista autorizou a assinatura recorrente
        // (webhook de preapproval, status "authorized").
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
