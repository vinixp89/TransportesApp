namespace TransportesApp.Application.DTOs
{
    public record CriarClienteRequest(
        string Nome,
        string Telefone,
        string Email,
        string Logradouro,
        string Numero,
        string Bairro,
        string Cidade,
        string Estado,
        double Latitude,
        double Longitude,
        string? Complemento
    );

    public record ClienteResponse(
        Guid Id,
        string Nome,
        string Telefone,
        string Email,
        double? AvaliacaoMedia,
        DateTime DataCadastro
    );
}
