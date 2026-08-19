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
        string? Complemento = null,
        double? Latitude = null,
        double? Longitude = null
    );

    public record ClienteResponse(
      Guid Id,
      Guid UsuarioId,
      string Nome,
      string Telefone,
      string Email,
      double? AvaliacaoMedia,
      DateTime DataCadastro
  );
}
