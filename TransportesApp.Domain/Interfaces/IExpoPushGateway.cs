namespace TransportesApp.Domain.Interfaces
{
    public interface IExpoPushGateway
    {
        // Retorna os tokens que o Expo reportou como inválidos (app desinstalado, token revogado)
        // pra quem chamou poder limpar do banco — nunca lança por token individual falhar, só por
        // erro de transporte/HTTP com o serviço do Expo.
        Task<IReadOnlyList<string>> EnviarAsync(IReadOnlyList<string> tokens, string titulo, string mensagem);
    }
}
