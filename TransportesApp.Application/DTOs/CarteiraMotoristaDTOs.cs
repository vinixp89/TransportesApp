using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // ValorMinimoSaque vem junto pra o app não precisar hardcodar a regra — ver
    // CarteiraMotoristaService.ValorMinimoSaque.
    //
    // Saldo é o total da carteira (inclui o bônus de boas-vindas ainda travado, ver BonusMotorista);
    // SaldoBloqueado é a parte dele que ainda não pode ser sacada e SaldoDisponivel = Saldo - SaldoBloqueado.
    public record CarteiraMotoristaResponse(
        Guid Id, Guid MotoristaId, decimal Saldo, decimal ValorMinimoSaque, DateTime DataCriacao,
        decimal SaldoBloqueado = 0, decimal SaldoDisponivel = 0);

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
        Guid MotoristaId,
        string MotoristaNome,
        string MotoristaCpf,
        string MotoristaTelefone,
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
