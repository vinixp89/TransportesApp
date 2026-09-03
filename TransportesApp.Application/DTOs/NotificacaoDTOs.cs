using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record NotificacaoResponse(
        Guid Id,
        string Titulo,
        string Mensagem,
        TipoNotificacao Tipo,
        bool Lida,
        DateTime DataCriacao
    );

    public record ContagemNaoLidasResponse(int NaoLidas);
}
