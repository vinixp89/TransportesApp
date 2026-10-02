using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record EnviarMensagemSuporteRequest(string Texto);

    public record MensagemSuporteResponse(
        Guid Id,
        Guid UsuarioId,
        TipoUsuario TipoUsuario,
        bool EnviadaPeloAdmin,
        string Texto,
        DateTime DataEnvio
    );

    // Uma linha por conversa, pro Admin ver de quem é cada uma (nome) e se já respondeu a última
    // mensagem ou se ainda está pendente — sem precisar abrir a conversa inteira só pra isso.
    public record ConversaSuporteResponse(
        Guid UsuarioId,
        TipoUsuario TipoUsuario,
        string NomeUsuario,
        string UltimoTexto,
        DateTime UltimaData,
        bool PendenteResposta
    );
}
