namespace TransportesApp.Application.DTOs
{
    public record RegistrarClienteRequest(string Email, string Senha, CriarClienteRequest Cliente);

    public record RegistrarMotoristaRequest(string Email, string Senha, CriarMotoristaRequest Motorista);

    public record LoginRequest(string Email, string Senha);

    public record EsqueciSenhaRequest(string Email);

    public record RedefinirSenhaRequest(string Email, string Codigo, string NovaSenha);

    public record AuthResponse(string Token, DateTime ExpiraEm, string Email, Guid UsuarioId);
}
