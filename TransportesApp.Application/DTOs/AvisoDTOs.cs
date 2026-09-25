namespace TransportesApp.Application.DTOs
{
    public record AvisoResponse(
        Guid Id,
        string Titulo,
        string Texto,
        string CorFundoHex,
        string? TextoBotao,
        string? TelaDestino,
        DateTime DataInicio,
        DateTime DataFim,
        bool Ativo);

    public record CriarAvisoRequest(
        string Titulo,
        string Texto,
        string CorFundoHex,
        string? TextoBotao,
        string? TelaDestino,
        DateTime DataInicio,
        DateTime DataFim);

    public record AtualizarAvisoRequest(
        string Titulo,
        string Texto,
        string CorFundoHex,
        string? TextoBotao,
        string? TelaDestino,
        DateTime DataInicio,
        DateTime DataFim,
        bool Ativo);
}
