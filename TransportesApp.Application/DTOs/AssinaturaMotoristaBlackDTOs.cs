using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // AnoVeiculo é autodeclarado pelo motorista — sem verificação de documento/foto nesta primeira
    // versão (ver Motorista.VeiculoElegivelParaBlack).
    public record AssinarBlackRequest(int AnoVeiculo);

    public record AssinaturaMotoristaBlackResponse(
        Guid Id,
        decimal PrecoMensal,
        DateTime DataInicio,
        StatusAssinatura Status
    );

    // Resposta de POST /Motoristas/black/assinar — CheckoutUrl vem preenchida quando o motorista
    // precisa ser redirecionado pro Mercado Pago pra confirmar o pagamento; vem null quando ele já
    // tinha uma assinatura Black ativa (nada a pagar de novo).
    public record AssinarBlackResponse(
        AssinaturaMotoristaBlackResponse Assinatura,
        string? CheckoutUrl
    );
}
