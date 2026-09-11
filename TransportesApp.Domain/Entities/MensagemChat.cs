using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Chat entre cliente e motorista durante uma corrida (ver MensagemChatService) — lido por
    // polling no app, mesmo princípio já usado pra corrida atual (sem SignalR/WebSocket). Fica
    // restrito à corrida em que foi enviada: não é um chat "geral" entre as duas contas.
    public class MensagemChat
    {
        public Guid Id { get; private set; }
        public Guid CorridaId { get; private set; }
        public TipoUsuario RemetenteTipo { get; private set; }
        public string Texto { get; private set; } = default!;
        public DateTime DataEnvio { get; private set; }

        public const int TamanhoMaximoTexto = 500;

        protected MensagemChat() { }

        public MensagemChat(Guid corridaId, TipoUsuario remetenteTipo, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Mensagem não pode ser vazia.");

            if (texto.Length > TamanhoMaximoTexto)
                throw new ArgumentException($"Mensagem não pode passar de {TamanhoMaximoTexto} caracteres.");

            Id = Guid.NewGuid();
            CorridaId = corridaId;
            RemetenteTipo = remetenteTipo;
            Texto = texto.Trim();
            DataEnvio = DateTime.UtcNow;
        }
    }
}
