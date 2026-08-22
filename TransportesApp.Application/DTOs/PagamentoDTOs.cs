using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Resultado de iniciar um pagamento — o chamador (ex: PlanoService.AssinarAsync) só precisa da
    // URL pra redirecionar o cliente; o Id existe caso queira guardar referência.
    public record PagamentoIniciadoResultado(Guid PagamentoId, string CheckoutUrl);

    public record PagamentoStatusResponse(Guid Id, StatusPagamento Status, decimal Valor, string Descricao);

    // Corpo que o Mercado Pago envia no POST do webhook — formato padrão "Webhooks" (não o antigo
    // IPN): { "type": "payment", "data": { "id": "123" } }. Todos os campos vêm em minúsculo no JSON,
    // o que já bate com a política camelCase padrão do ASP.NET Core, sem precisar de atributo nenhum.
    public record WebhookPayload(string? Type, string? Action, WebhookData? Data);

    public record WebhookData(string? Id);
}
