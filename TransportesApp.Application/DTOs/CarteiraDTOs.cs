using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    public record CarteiraResponse(Guid Id, Guid ClienteId, decimal Saldo, DateTime DataCriacao);

    public record RecarregarCarteiraRequest(decimal Valor);

    // A recarga não credita mais na hora — precisa confirmar o pagamento no Mercado Pago primeiro (ver
    // CarteiraService.IniciarRecargaAsync). O front redireciona pra CheckoutUrl, igual acontece com
    // planos pagos.
    public record IniciarRecargaResponse(string CheckoutUrl);

    public record TransacaoCarteiraResponse(Guid Id, TipoTransacaoCarteira Tipo, decimal Valor, DateTime Data, string Descricao);
}
