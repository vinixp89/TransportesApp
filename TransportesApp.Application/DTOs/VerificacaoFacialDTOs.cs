using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Listagem pro painel de revisão do Admin — MotoristaNome já resolvido, pra não precisar de mais
    // uma consulta na tela. Confere/Similaridade ficam null enquanto Processada for false (erro ou
    // ainda não comparado).
    public record VerificacaoFacialAdminResponse(
        Guid Id,
        Guid CorridaId,
        Guid MotoristaId,
        string MotoristaNome,
        MomentoVerificacaoFacial Momento,
        bool Processada,
        double? Similaridade,
        bool? Confere,
        string? ErroProcessamento,
        DateTime DataHora
    );
}
