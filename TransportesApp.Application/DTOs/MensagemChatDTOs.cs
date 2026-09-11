using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record EnviarMensagemChatRequest(string Texto);

    public record MensagemChatResponse(
        Guid Id,
        Guid CorridaId,
        TipoUsuario RemetenteTipo,
        string Texto,
        DateTime DataEnvio
    );
}
