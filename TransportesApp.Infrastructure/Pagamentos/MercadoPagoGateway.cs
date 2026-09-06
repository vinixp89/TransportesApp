using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Pagamentos
{
    // Implementação de IGatewayPagamento usando o SDK oficial do Mercado Pago (pacote NuGet
    // "mercadopago-sdk"). Usa Checkout Pro (preference + redirecionamento pro checkout hospedado do
    // Mercado Pago) em vez de tokenizar cartão dentro do nosso próprio app — assim a gente nunca chega
    // perto de dado de cartão de verdade, o que tira boa parte da complicação de segurança/PCI.
    //
    // MercadoPagoConfig.AccessToken é configurado uma vez só, no startup (Program.cs) — o SDK guarda
    // isso como estado estático global, então não tem por que ficar relendo a cada chamada.
    public class MercadoPagoGateway : IGatewayPagamento
    {
        public async Task<PreferenciaCriada> CriarPreferenciaAsync(SolicitacaoPagamento solicitacao)
        {
            GarantirAccessTokenConfigurado();

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = solicitacao.Descricao,
                        Quantity = 1,
                        CurrencyId = "BRL",
                        UnitPrice = solicitacao.Valor,
                    },
                },
                Payer = new PreferencePayerRequest { Email = solicitacao.EmailPagador },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = solicitacao.UrlRetornoSucesso,
                    Pending = solicitacao.UrlRetornoPendente,
                    Failure = solicitacao.UrlRetornoFalha,
                },
                // Sem URL pública configurada (ex: rodando só em localhost sem túnel), manda sem
                // notification_url — o Mercado Pago simplesmente não vai chamar webhook nenhum, e a
                // confirmação do pagamento acontece pela sincronização manual (ver PagamentosController).
                NotificationUrl = string.IsNullOrWhiteSpace(solicitacao.UrlNotificacao) ? null : solicitacao.UrlNotificacao,
                ExternalReference = solicitacao.ExternalReference,
                StatementDescriptor = "VAINABOA",
            };

            var client = new PreferenceClient();
            var preferencia = await client.CreateAsync(request);

            return new PreferenciaCriada(preferencia.Id, preferencia.InitPoint);
        }

        // Checkout API direto (POST /v1/payments com payment_method_id "pix"), sem Preference nem
        // redirecionamento — nossa conta não tem Pix disponível no Checkout Pro hospedado (confirmado
        // com o suporte do Mercado Pago), mas o Pix funciona normalmente por aqui, na mesma conta.
        public async Task<PagamentoPixCriado> CriarPagamentoPixAsync(SolicitacaoPagamentoPix solicitacao)
        {
            GarantirAccessTokenConfigurado();

            var request = new PaymentCreateRequest
            {
                TransactionAmount = solicitacao.Valor,
                Description = solicitacao.Descricao,
                PaymentMethodId = "pix",
                Payer = new PaymentPayerRequest
                {
                    Email = solicitacao.EmailPagador,
                    Identification = new IdentificationRequest
                    {
                        Type = "CPF",
                        Number = solicitacao.CpfPagador,
                    },
                },
                ExternalReference = solicitacao.ExternalReference,
                NotificationUrl = string.IsNullOrWhiteSpace(solicitacao.UrlNotificacao) ? null : solicitacao.UrlNotificacao,
                StatementDescriptor = "VAINABOA",
            };

            var client = new PaymentClient();
            var pagamento = await client.CreateAsync(request);

            var dadosPix = pagamento.PointOfInteraction?.TransactionData
                ?? throw new InvalidOperationException("Mercado Pago não devolveu os dados do QR Code Pix.");

            return new PagamentoPixCriado(
                pagamento.Id!.Value.ToString(),
                MapearStatus(pagamento.Status),
                dadosPix.QrCode!,
                dadosPix.QrCodeBase64!
            );
        }

        // Estorno integral (sem informar valor = devolve o total, conforme a API do Mercado Pago).
        public async Task EstornarPagamentoAsync(string pagamentoGatewayId)
        {
            GarantirAccessTokenConfigurado();

            if (!long.TryParse(pagamentoGatewayId, out var id))
                throw new ArgumentException($"Id de pagamento do Mercado Pago inválido: \"{pagamentoGatewayId}\".", nameof(pagamentoGatewayId));

            var client = new PaymentRefundClient();
            await client.CreateAsync(id);
        }

        public async Task<StatusPagamentoGateway> ConsultarPagamentoAsync(string pagamentoGatewayId)
        {
            GarantirAccessTokenConfigurado();

            if (!long.TryParse(pagamentoGatewayId, out var id))
                throw new ArgumentException($"Id de pagamento do Mercado Pago inválido: \"{pagamentoGatewayId}\".", nameof(pagamentoGatewayId));

            var client = new PaymentClient();
            var pagamento = await client.GetAsync(id);

            return new StatusPagamentoGateway(
                pagamento.Id!.Value.ToString(),
                MapearStatus(pagamento.Status),
                pagamento.ExternalReference,
                pagamento.TransactionAmount
            );
        }

        // Vocabulário de status de pagamento do Mercado Pago — ver
        // https://www.mercadopago.com.br/developers/pt/docs/checkout-api/payment-management/payment-status
        private static StatusPagamento MapearStatus(string? statusBruto) => statusBruto switch
        {
            "approved" => StatusPagamento.Aprovado,
            "pending" => StatusPagamento.Pendente,
            "in_process" or "authorized" => StatusPagamento.EmProcessamento,
            "rejected" => StatusPagamento.Recusado,
            "cancelled" => StatusPagamento.Cancelado,
            "refunded" or "charged_back" => StatusPagamento.Estornado,
            // Qualquer status novo que o Mercado Pago venha a criar cai aqui como Pendente em vez de
            // quebrar — mais seguro que assumir Aprovado/Recusado num caso desconhecido.
            _ => StatusPagamento.Pendente,
        };

        private static void GarantirAccessTokenConfigurado()
        {
            if (string.IsNullOrWhiteSpace(MercadoPagoConfig.AccessToken))
                throw new InvalidOperationException(
                    "Access Token do Mercado Pago não configurado (MercadoPago:AccessToken nos User Secrets — ver Program.cs).");
        }
    }
}
