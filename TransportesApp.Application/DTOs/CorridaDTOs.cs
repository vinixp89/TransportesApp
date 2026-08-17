using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record EnderecoRequest
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

    public record CriarCorridasRequest
        (
            Guid ClienteId,
            EnderecoRequest Origem,
            EnderecoRequest Destino,
            double DistanciaEstimadaKm,
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
            DateTime DataSolicitacao
        );
}