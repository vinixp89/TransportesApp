using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record CarteiraResponse(Guid Id, Guid ClienteId, decimal Saldo, DateTime DataCriacao);

    public record RecarregarCarteiraRequest(decimal Valor);

    public record TransacaoCarteiraResponse(Guid Id, TipoTransacaoCarteira Tipo, decimal Valor, DateTime Data, string Descricao);
}
