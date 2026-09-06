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

        // Cria um pagamento Pix direto (Checkout API, sem redirecionar pra nenhuma página do
        // gateway) e devolve o QR Code pronto pra mostrar na hora — usado quando o Checkout Pro
        // (CriarPreferenciaAsync) não oferece Pix como opção pra determinada conta. A confirmação do
        // pagamento usa o MESMO fluxo de sempre (webhook/sincronização manual via
        // ConsultarPagamentoAsync), já que aqui a gente já sai com o Id do pagamento no gateway.
        Task<PagamentoPixCriado> CriarPagamentoPixAsync(SolicitacaoPagamentoPix solicitacao);

        // Estorna (reembolsa) integralmente um pagamento já aprovado — usado quando o cliente cancela
        // uma corrida avulsa que já foi paga de verdade. pagamentoGatewayId é o Id que O GATEWAY deu
        // ao pagamento (o mesmo usado em ConsultarPagamentoAsync), não o nosso Pagamento.Id.
        Task EstornarPagamentoAsync(string pagamentoGatewayId);
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

    public sealed record SolicitacaoPagamentoPix(
        string ExternalReference,
        string Descricao,
        decimal Valor,
        string EmailPagador,
        // Pix pelo Mercado Pago exige o CPF de quem paga (identification.number) — sem isso a API
        // recusa a criação do pagamento.
        string CpfPagador,
        string? UrlNotificacao
    );

    public sealed record PagamentoPixCriado(
        string PagamentoGatewayId,
        StatusPagamento Status,
        // "Copia e cola" (EMV do Pix) — o que o app mostra pra copiar, e também o que dá pra
        // transformar num QR Code do lado do cliente se quiser.
        string QrCodeCopiaCola,
        // Imagem do QR Code já pronta, em base64 (PNG) — o Mercado Pago gera ela pronta, não precisa
        // de biblioteca de QR Code nenhuma no nosso lado.
        string QrCodeBase64
    );
}
