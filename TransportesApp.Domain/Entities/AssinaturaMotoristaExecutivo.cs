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
        // URL de checkout devolvida pelo Mercado Pago junto com o PreapprovalId — guardada aqui porque,
        // com a aprovação manual (ver Aprovar), quem cria o Preapproval é o Admin, não o motorista; o
        // motorista só vê essa URL depois, consultando MinhaAssinaturaExecutivo.
        public string? CheckoutUrl { get; private set; }
        // E-mail informado pelo motorista no momento da solicitação — guardado aqui (em vez de só
        // passado direto pro gateway como antes) porque quem efetivamente cria a cobrança agora é o
        // Admin, numa requisição totalmente separada (ver AprovarAsync).
        public string EmailPagador { get; private set; } = default!;
        // Preenchido só quando o Admin nega a solicitação (ver Negar) — mostrado ao motorista pra ele
        // entender o motivo.
        public string? MotivoNegacao { get; private set; }

        protected AssinaturaMotoristaExecutivo() { }

        public AssinaturaMotoristaExecutivo(Guid motoristaId, string emailPagador)
        {
            if (string.IsNullOrWhiteSpace(emailPagador))
                throw new ArgumentException("E-mail do pagador é obrigatório.", nameof(emailPagador));

            Id = Guid.NewGuid();
            MotoristaId = motoristaId;
            EmailPagador = emailPagador;
            DataInicio = DateTime.UtcNow;
            Status = StatusAssinatura.AguardandoAprovacao;
        }

        // Chamado pelo Admin depois de conferir a placa/foto do carro manualmente (ver
        // AssinaturaMotoristaExecutivoService.AprovarAsync) — só então a cobrança é criada de verdade
        // no Mercado Pago.
        public void Aprovar()
        {
            if (Status != StatusAssinatura.AguardandoAprovacao)
                throw new InvalidOperationException($"Só é possível aprovar uma solicitação aguardando aprovação (status atual: {Status}).");

            Status = StatusAssinatura.PendentePagamento;
        }

        // Chamado pelo Admin quando a placa/modelo declarado não bate com o que ele verificou (ver
        // AssinaturaMotoristaExecutivoService.NegarAsync). Diferente de Cancelar: aqui nunca chegou a
        // existir Preapproval no gateway, então não tem nada pra cancelar lá.
        public void Negar(string motivo)
        {
            if (Status != StatusAssinatura.AguardandoAprovacao)
                throw new InvalidOperationException($"Só é possível negar uma solicitação aguardando aprovação (status atual: {Status}).");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Informe o motivo da negativa.", nameof(motivo));

            Status = StatusAssinatura.NegadaAdmin;
            MotivoNegacao = motivo;
            DataCancelamento = DateTime.UtcNow;
        }

        // Chamado assim que o Preapproval é criado no Mercado Pago (ver
        // AssinaturaMotoristaExecutivoService.AprovarAsync) — antes ainda do motorista autorizar.
        public void RegistrarPreapproval(string preapprovalId, string checkoutUrl)
        {
            PreapprovalId = preapprovalId;
            CheckoutUrl = checkoutUrl;
        }

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
            if (Status is not (StatusAssinatura.Ativa or StatusAssinatura.PendentePagamento or StatusAssinatura.AguardandoAprovacao))
                throw new InvalidOperationException($"Essa assinatura não pode ser cancelada (status atual: {Status}).");

            Status = StatusAssinatura.Cancelada;
            DataCancelamento = DateTime.UtcNow;
        }
    }
}
