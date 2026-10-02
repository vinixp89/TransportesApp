using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Chat entre Cliente/Motorista e o Admin pra tirar dúvida ou resolver problema fora do contexto
    // de uma corrida específica (ver MensagemChat, que é por corrida) — mesmo princípio de polling,
    // sem SignalR/WebSocket. Uma conversa por usuário: todas as mensagens de um mesmo UsuarioId
    // formam uma única thread contínua, independente de quantas vezes ele volte a falar com o
    // suporte.
    public class MensagemSuporte
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public TipoUsuario TipoUsuario { get; private set; }
        public bool EnviadaPeloAdmin { get; private set; }
        public string Texto { get; private set; } = default!;
        public DateTime DataEnvio { get; private set; }

        public const int TamanhoMaximoTexto = 500;

        protected MensagemSuporte() { }

        public MensagemSuporte(Guid usuarioId, TipoUsuario tipoUsuario, bool enviadaPeloAdmin, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Mensagem não pode ser vazia.");

            if (texto.Length > TamanhoMaximoTexto)
                throw new ArgumentException($"Mensagem não pode passar de {TamanhoMaximoTexto} caracteres.");

            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            TipoUsuario = tipoUsuario;
            EnviadaPeloAdmin = enviadaPeloAdmin;
            Texto = texto.Trim();
            DataEnvio = DateTime.UtcNow;
        }
    }
}
