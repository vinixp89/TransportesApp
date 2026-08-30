using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // AnoVeiculo é autodeclarado pelo motorista — sem verificação de documento/foto nesta primeira
    // versão (ver Motorista.VeiculoElegivelParaExecutivo).
    public record AssinarExecutivoRequest(int AnoVeiculo);

    public record AssinaturaMotoristaExecutivoResponse(
        Guid Id,
        decimal PrecoMensal,
        DateTime DataInicio,
        StatusAssinatura Status
    );

    // Resposta de POST /Motoristas/executivo/assinar — CheckoutUrl vem preenchida quando o motorista
    // precisa ser redirecionado pro Mercado Pago pra confirmar o pagamento; vem null quando ele já
    // tinha uma assinatura Executivo ativa (nada a pagar de novo).
    public record AssinarExecutivoResponse(
        AssinaturaMotoristaExecutivoResponse Assinatura,
        string? CheckoutUrl
    );
}
