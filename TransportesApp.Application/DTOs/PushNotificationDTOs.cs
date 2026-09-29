using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record RegistrarPushTokenRequest(string Token);

    public record EnviarBroadcastRequest(TipoUsuario Papel, string Titulo, string Mensagem);

    public record BroadcastEnviadoResponse(int TotalEnviado);
}
