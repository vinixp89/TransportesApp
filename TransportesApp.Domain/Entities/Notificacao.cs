using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Notificação in-app (caixa de entrada) — separada das notificações push (Expo/Firebase, que só
    // avisam o celular na hora). Essa aqui fica persistida, então dá pra ver o histórico completo
    // dentro do app, mesmo que o push tenha falhado ou o celular estivesse desligado na hora.
    // Sempre pra um Cliente OU um Motorista, nunca os dois — ver ParaCliente/ParaMotorista.
    public class Notificacao
    {
        public Guid Id { get; private set; }
        public Guid? ClienteId { get; private set; }
        public Guid? MotoristaId { get; private set; }
        public string Titulo { get; private set; } = default!;
        public string Mensagem { get; private set; } = default!;
        public TipoNotificacao Tipo { get; private set; }
        public bool Lida { get; private set; }
        public DateTime DataCriacao { get; private set; }

        protected Notificacao() { }

        private Notificacao(Guid? clienteId, Guid? motoristaId, string titulo, string mensagem, TipoNotificacao tipo)
        {
            if (string.IsNullOrWhiteSpace(titulo))
                throw new ArgumentException("Título é obrigatório.");

            if (string.IsNullOrWhiteSpace(mensagem))
                throw new ArgumentException("Mensagem é obrigatória.");

            Id = Guid.NewGuid();
            ClienteId = clienteId;
            MotoristaId = motoristaId;
            Titulo = titulo;
            Mensagem = mensagem;
            Tipo = tipo;
            Lida = false;
            DataCriacao = DateTime.UtcNow;
        }

        public static Notificacao ParaCliente(Guid clienteId, string titulo, string mensagem, TipoNotificacao tipo)
            => new(clienteId, null, titulo, mensagem, tipo);

        public static Notificacao ParaMotorista(Guid motoristaId, string titulo, string mensagem, TipoNotificacao tipo)
            => new(null, motoristaId, titulo, mensagem, tipo);

        public void MarcarComoLida() => Lida = true;
    }
}
