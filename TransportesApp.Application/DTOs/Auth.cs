namespace TransportesApp.Application.DTOs
{
    public record RegistrarClienteRequest(string Email, string Senha, CriarClienteRequest Cliente);

    public record RegistrarMotoristaRequest(string Email, string Senha, CriarMotoristaRequest Motorista);

    public record LoginRequest(string Email, string Senha);

    public record EsqueciSenhaRequest(string Email);

    public record RedefinirSenhaRequest(string Email, string Codigo, string NovaSenha);

    // Diferente de RedefinirSenha (fluxo "esqueci minha senha", via código por e-mail, sem estar
    // logado) — aqui o usuário já está logado e confirma a senha atual, sem precisar de código.
    public record TrocarSenhaRequest(string SenhaAtual, string NovaSenha);

    public record AuthResponse(string Token, DateTime ExpiraEm, string Email, Guid UsuarioId);
}
