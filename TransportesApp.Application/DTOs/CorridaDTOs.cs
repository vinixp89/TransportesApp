using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Latitude/Longitude não são informadas pelo cliente: o backend sempre descobre sozinho a
    // partir do endereço em texto, usando o Geocoding do Google Maps.
    public record EnderecoRequest
        (
            string Logradouro,
            string Numero,
            string Bairro,
            string Cidade,
            string Estado,
            string? Complemento = null
        );

    // A distância não vem mais do cliente — o backend calcula via Google Maps a partir de Origem/Destino,
    // pra evitar que alguém manipule a requisição e informe uma distância falsa pra pagar menos.
    public record CriarCorridasRequest
        (
            EnderecoRequest Origem,
            EnderecoRequest Destino,
            TipoConsumo TipoConsumo,
            Guid? PacoteCorridasId
        );

    public record EnderecoResponse
        (
            string Logradouro,
            string Numero,
            string Bairro,
            string Cidade,
            string Estado,
            double Latitude,
            double Longitude,
            string? Complemento
        );

    public record CorridaResponse
        (
            Guid Id,
            Guid ClienteId,
            Guid? MotoristaId,
            EnderecoResponse Origem,
            EnderecoResponse Destino,
            double DistanciaEstimadaKm,
            double? DistanciaRealKm,
            CorFaixa FaixaContratada,
            TipoConsumo TipoConsumo,
            StatusCorrida Status,
            DateTime DataSolicitacao,
           IReadOnlyList<string>? AvisosEndereco = null
        );

    public record FinalizarCorridaRequest(double DistanciaReal);

    public record FinalizarCorridaResponse(bool EstouroFaixa, CorridaResponse Corrida);
}