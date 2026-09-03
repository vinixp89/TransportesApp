using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // ValorMinimoSaque vem junto pra o app não precisar hardcodar a regra — ver
    // CarteiraMotoristaService.ValorMinimoSaque.
    public record CarteiraMotoristaResponse(Guid Id, Guid MotoristaId, decimal Saldo, decimal ValorMinimoSaque, DateTime DataCriacao);

    public record TransacaoCarteiraMotoristaResponse(Guid Id, TipoTransacaoCarteiraMotorista Tipo, decimal Valor, DateTime Data, string Descricao);

    public record SolicitarSaqueRequest(
        decimal Valor,
        TipoSaque Tipo,
        string? ChavePix,
        string? Banco,
        string? Agencia,
        string? Conta,
        string? TipoConta);

    public record SolicitacaoSaqueResponse(
        Guid Id,
        decimal Valor,
        TipoSaque Tipo,
        string? ChavePix,
        string? Banco,
        string? Agencia,
        string? Conta,
        string? TipoConta,
        StatusSolicitacaoSaque Status,
        DateTime DataSolicitacao,
        DateTime? DataProcessamento,
        string? MotivoRejeicao);

    public record RejeitarSaqueRequest(string Motivo);
}
