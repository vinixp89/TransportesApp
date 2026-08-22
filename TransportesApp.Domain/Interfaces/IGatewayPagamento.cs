using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Interfaces
{
    // Abstração sobre "qual gateway de pagamento a gente usa" — hoje só existe MercadoPagoGateway
    // (TransportesApp.Infrastructure/Pagamentos), mas o Domain/Application nunca falam com o SDK do
    // Mercado Pago diretamente, só com essa interface. Troca de gateway (ou um gateway "fake" pra
    // teste) vira só uma implementação nova, sem mexer em PagamentoService.
    public interface IGatewayPagamento
    {
        // Cria a cobrança no gateway (no Mercado Pago, uma "preference" de Checkout Pro) e devolve a
        // URL de checkout pra redirecionar o cliente. ExternalReference deve ser o Pagamento.Id (como
        // string) — é assim que ConsultarPagamentoAsync consegue achar de volta o Pagamento
        // correspondente quando o gateway avisa que algo mudou.
        Task<PreferenciaCriada> CriarPreferenciaAsync(SolicitacaoPagamento solicitacao);

        // Consulta o status atual de um pagamento no gateway a partir do Id que O PRÓPRIO GATEWAY deu
        // a ele (não é o Pagamento.Id nosso) — usado pelo webhook/sincronização manual. Nunca confia
        // no corpo da notificação em si, sempre busca de volta na API do gateway.
        Task<StatusPagamentoGateway> ConsultarPagamentoAsync(string pagamentoGatewayId);
    }

    public sealed record SolicitacaoPagamento(
        string ExternalReference,
        string Descricao,
        decimal Valor,
        string EmailPagador,
        string UrlRetornoSucesso,
        string UrlRetornoPendente,
        string UrlRetornoFalha,
        // Null quando não há URL pública configurada (ex: rodando local sem túnel) — nesse caso o
        // gateway simplesmente não pede notificação nenhuma, ver MercadoPagoGateway.
        string? UrlNotificacao
    );

    public sealed record PreferenciaCriada(
        string PreferenceId,
        string UrlCheckout
    );

    // Status já vem TRADUZIDO pro nosso StatusPagamento — quem faz essa tradução (ex: "approved" →
    // StatusPagamento.Aprovado) é a implementação do gateway (MercadoPagoGateway.MapearStatus), nunca
    // quem chama essa interface, porque o vocabulário de status é específico de cada provedor.
    public sealed record StatusPagamentoGateway(
        string PagamentoGatewayId,
        StatusPagamento Status,
        string? ExternalReference,
        decimal? Valor
    );
}
