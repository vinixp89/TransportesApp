namespace TransportesApp.Application.DTOs
{
    public record CriarMotoristaRequest(
        string Cnh,
        string Cpf,
        string PlacaVeiculo,
        string ModeloVeiculo,
        string Logradouro,
        string Numero,
        string Bairro,
        string Cidade,
        string Estado,
        string? Complemento = null,
        double? Latitude = null,
        double? Longitude = null
    );

    public record MotoristaResponse(
        Guid Id,
        Guid UsuarioId,
        string Cnh,
        string Cpf,
        string PlacaVeiculo,
        string ModeloVeiculo,
        double AvaliacaoMeida,
        DateTime DataCadastro,
        EnderecoResponse Endereco
    );
}
