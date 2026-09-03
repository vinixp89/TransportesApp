using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Pedido de saque do saldo do motorista. Não existe integração com gateway pra saque (só pra
    // cobrança, via Mercado Pago) — então o valor é debitado da CarteiraMotorista assim que a
    // solicitação é criada (evita pedir o mesmo saldo duas vezes) e ela nasce Pendente até alguém do
    // Admin transferir o dinheiro de verdade (Pix/TED manual) e marcar como Concluida, ou Rejeitar
    // (o que estorna o valor de volta pro saldo — ver CarteiraMotoristaService).
    public class SolicitacaoSaque
    {
        public Guid Id { get; private set; }
        public Guid MotoristaId { get; private set; }
        public decimal Valor { get; private set; }
        public TipoSaque Tipo { get; private set; }

        // Preenchido quando Tipo == Pix.
        public string? ChavePix { get; private set; }

        // Preenchidos quando Tipo == TransferenciaBancaria.
        public string? Banco { get; private set; }
        public string? Agencia { get; private set; }
        public string? Conta { get; private set; }
        public string? TipoConta { get; private set; }

        public StatusSolicitacaoSaque Status { get; private set; }
        public DateTime DataSolicitacao { get; private set; }
        public DateTime? DataProcessamento { get; private set; }
        public string? MotivoRejeicao { get; private set; }

        protected SolicitacaoSaque() { }

        public SolicitacaoSaque(
            Guid motoristaId,
            decimal valor,
            TipoSaque tipo,
            string? chavePix,
            string? banco,
            string? agencia,
            string? conta,
            string? tipoConta)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do saque precisa ser maior que zero.");

            if (tipo == TipoSaque.Pix && string.IsNullOrWhiteSpace(chavePix))
                throw new ArgumentException("Informe a chave Pix.");

            if (tipo == TipoSaque.TransferenciaBancaria &&
                (string.IsNullOrWhiteSpace(banco) || string.IsNullOrWhiteSpace(agencia) ||
                 string.IsNullOrWhiteSpace(conta) || string.IsNullOrWhiteSpace(tipoConta)))
                throw new ArgumentException("Informe banco, agência, conta e tipo de conta.");

            Id = Guid.NewGuid();
            MotoristaId = motoristaId;
            Valor = valor;
            Tipo = tipo;
            ChavePix = tipo == TipoSaque.Pix ? chavePix : null;
            Banco = tipo == TipoSaque.TransferenciaBancaria ? banco : null;
            Agencia = tipo == TipoSaque.TransferenciaBancaria ? agencia : null;
            Conta = tipo == TipoSaque.TransferenciaBancaria ? conta : null;
            TipoConta = tipo == TipoSaque.TransferenciaBancaria ? tipoConta : null;
            Status = StatusSolicitacaoSaque.Pendente;
            DataSolicitacao = DateTime.UtcNow;
        }

        public void Concluir()
        {
            if (Status != StatusSolicitacaoSaque.Pendente)
                throw new InvalidOperationException("Essa solicitação já foi processada.");

            Status = StatusSolicitacaoSaque.Concluida;
            DataProcessamento = DateTime.UtcNow;
        }

        public void Rejeitar(string motivo)
        {
            if (Status != StatusSolicitacaoSaque.Pendente)
                throw new InvalidOperationException("Essa solicitação já foi processada.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Informe o motivo da rejeição.");

            Status = StatusSolicitacaoSaque.Rejeitada;
            DataProcessamento = DateTime.UtcNow;
            MotivoRejeicao = motivo;
        }
    }
}
