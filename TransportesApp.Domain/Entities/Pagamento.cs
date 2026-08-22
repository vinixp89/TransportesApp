using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Registro de uma cobrança feita através de um gateway de pagamento (hoje só Mercado Pago, ver
    // IGatewayPagamento). Genérico de propósito — TipoReferencia + ReferenciaId apontam pra "o que"
    // está sendo pago (uma AssinaturaPlano, um PacoteCorridas, uma Corrida...) sem essa entidade
    // precisar conhecer os detalhes de cada feature. Quem decide o que fazer quando o pagamento é
    // aprovado é o PagamentoService, olhando o TipoReferencia.
    public class Pagamento
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public TipoReferenciaPagamento TipoReferencia { get; private set; }
        public Guid ReferenciaId { get; private set; }
        public decimal Valor { get; private set; }
        public string Descricao { get; private set; } = default!;
        public StatusPagamento Status { get; private set; }

        // Id da preference criada no Mercado Pago (gerado ao iniciar o checkout) — usado só pra
        // rastreabilidade, a correlação de volta pra esse Pagamento é feita pelo ExternalReference
        // (= este Id.ToString()) que a gente manda pro gateway na hora de criar a preference.
        public string? PreferenceId { get; private set; }

        // Id do pagamento em si dentro do Mercado Pago — só existe depois que o cliente efetivamente
        // tenta pagar (aprovado, recusado, em análise etc.); fica null enquanto ninguém pagou ainda.
        public string? PagamentoGatewayId { get; private set; }

        public DateTime DataCriacao { get; private set; }
        public DateTime? DataAtualizacao { get; private set; }

        protected Pagamento() { }

        public Pagamento(Guid clienteId, TipoReferenciaPagamento tipoReferencia, Guid referenciaId, decimal valor, string descricao)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do pagamento precisa ser maior que zero.", nameof(valor));

            Id = Guid.NewGuid();
            ClienteId = clienteId;
            TipoReferencia = tipoReferencia;
            ReferenciaId = referenciaId;
            Valor = valor;
            Descricao = descricao;
            Status = StatusPagamento.Pendente;
            DataCriacao = DateTime.UtcNow;
        }

        public void RegistrarPreference(string preferenceId)
        {
            PreferenceId = preferenceId;
            DataAtualizacao = DateTime.UtcNow;
        }

        // Idempotente por fora (ver PagamentoService.ProcessarNotificacaoAsync, que só chama isso
        // quando o status realmente mudou) — o Mercado Pago pode reenviar a mesma notificação mais
        // de uma vez, então quem chama essa entidade é quem garante não duplicar efeito colateral.
        public void AtualizarStatus(StatusPagamento novoStatus, string pagamentoGatewayId)
        {
            // Cancelado/Estornado são finais de verdade (nada muda depois). Aprovado NÃO é bloqueado
            // aqui de propósito — um pagamento aprovado pode virar Estornado depois (reembolso/chargeback).
            if (Status is StatusPagamento.Cancelado or StatusPagamento.Estornado)
                throw new InvalidOperationException($"Pagamento já está em um estado final ({Status}) e não pode mudar pra {novoStatus}.");

            Status = novoStatus;
            PagamentoGatewayId = pagamentoGatewayId;
            DataAtualizacao = DateTime.UtcNow;
        }
    }
}
