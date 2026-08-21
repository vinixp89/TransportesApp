using TransportesApp.Domain.Enums;

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
        EnderecoResponse Endereco,
        StatusMotorista Status,
        double? LatitudeAtual,
        double? LongitudeAtual
    );

    // Versão enxuta, sem CPF/CNH, pra um cliente ver motoristas disponíveis sem expor dados sensíveis de outra pessoa.
    public record MotoristaDisponivelResponse(
        Guid Id,
        string PlacaVeiculo,
        string ModeloVeiculo,
        double AvaliacaoMedia,
        double? LatitudeAtual,
        double? LongitudeAtual,
        double? DistanciaKm
    );

    public record AtualizarLocalizacaoRequest(double Latitude, double Longitude);
}
