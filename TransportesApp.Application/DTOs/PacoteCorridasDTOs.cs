using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Quantidade precisa ser 3, 5 ou 10 (FaixaDistancia.TamanhosPacoteDisponiveis).
    public record CriarPacoteCorridasRequest(CorFaixa Faixa, int Quantidade);

    public record PacoteCorridasResponse
        (
            Guid Id,
            Guid ClienteId,
            CorFaixa Faixa,
            int QuantidadeTotal,
            int QuantidadeUsada,
            int QuantidadeRestante,
            decimal PrecoPago,
            DateTime DataCompra
        );
}
