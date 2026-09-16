using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record AvaliarCorridaRequest(int Nota, string? Comentario);

    public record AvaliacaoResponse(
        Guid Id,
        Guid CorridaId,
        TipoUsuario AutorTipo,
        int Nota,
        string? Comentario,
        DateTime DataAvaliacao
    );
}
