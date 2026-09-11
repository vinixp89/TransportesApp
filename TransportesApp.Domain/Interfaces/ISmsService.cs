namespace TransportesApp.Domain.Interfaces
{
    // Abstração sobre "qual provedor de SMS a gente usa" — hoje só existe TwilioSmsService
    // (TransportesApp.Infrastructure/Sms), mesmo princípio do IEmailService/IGatewayPagamento: o
    // resto do sistema nunca fala com o SDK do provedor diretamente, só com essa interface.
    public interface ISmsService
    {
        // numeroDestino pode vir em qualquer formato brasileiro comum (com ou sem DDI, com ou sem
        // formatação) — normalizar pro formato que o provedor exige (E.164 no caso do Twilio) é
        // responsabilidade de quem implementa, não de quem chama.
        Task EnviarAsync(string numeroDestino, string mensagem);
    }
}
