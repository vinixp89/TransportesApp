namespace TransportesApp.Application.DTOs
{
    public record SugestaoEnderecoResponse(string PlaceId, string Descricao);

    // Já vem pronto no formato que o front usa pra montar o EnderecoRequest — Numero pode vir nulo
    // quando o local não tem número (ex: uma praça), o front decide o que fazer nesse caso.
    public record DetalhesEnderecoResponse(
        string Logradouro,
        string? Numero,
        string? Bairro,
        string Cidade,
        string Estado,
        double Latitude,
        double Longitude
    );
}
