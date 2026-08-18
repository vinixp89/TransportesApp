namespace TransportesApp.Application.DTOs
{
    public record RegistrarRequest(string Email, string Senha);

    public record LoginRequest(string Email, string Senha);

    public record AuthResponse(string Token, DateTime ExpiraEm, string Email, Guid UsuarioId);
}