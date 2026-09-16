using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // AnoVeiculo é autodeclarado pelo motorista — sem verificação de documento/foto nesta primeira
    // versão (ver Motorista.VeiculoElegivelParaExecutivo).
    public record AssinarExecutivoRequest(int AnoVeiculo);

    // CheckoutUrl só vem preenchida depois que o Admin aprova a solicitação (ver
    // AssinaturaMotoristaExecutivoService.AprovarAsync) — status AguardandoAprovacao nunca tem.
    // MotivoNegacao só vem preenchido quando Status é NegadaAdmin.
    public record AssinaturaMotoristaExecutivoResponse(
        Guid Id,
        decimal PrecoMensal,
        DateTime DataInicio,
        StatusAssinatura Status,
        string? CheckoutUrl,
        string? MotivoNegacao
    );

    // Resposta de POST /Motoristas/executivo/assinar — a solicitação sempre nasce AguardandoAprovacao
    // agora (ver AssinaturaMotoristaExecutivoService.AssinarAsync), então CheckoutUrl aqui só vem
    // preenchida no caso de já existir assinatura ativa (nada a aprovar) — segue null no caminho normal,
    // até o Admin aprovar (o motorista descobre consultando MinhaAssinaturaExecutivo depois).
    public record AssinarExecutivoResponse(
        AssinaturaMotoristaExecutivoResponse Assinatura,
        string? CheckoutUrl
    );

    // Fila de revisão do Admin (GET /admin/executivo/pendentes) — dados do veículo declarado + link
    // das fotos, pra ele conferir a placa manualmente antes de aprovar ou negar.
    public record SolicitacaoExecutivoAdminResponse(
        Guid AssinaturaId,
        Guid MotoristaId,
        string PlacaVeiculo,
        string ModeloVeiculo,
        int? AnoVeiculo,
        bool TemFotoVeiculo,
        bool TemFotoPlaca,
        DateTime DataSolicitacao
    );

    public record NegarExecutivoRequest(string Motivo);
}
