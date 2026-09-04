using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Pagamentos
{
    // Implementação de IGatewayPagamentoSaque usando a API Banking do Banco Inter — envia o Pix de
    // verdade pro motorista quando o Admin conclui um saque (ver CarteiraMotoristaService).
    // Documentação oficial: https://developers.inter.co/references/banking ("Autenticação OAuth" e
    // "Pix Pagamento" — endpoints e contrato conferidos direto na doc, sem chute).
    //
    // Autenticação é OAuth2 client_credentials + mTLS (certificado cliente configurado no HttpClient,
    // ver Program.cs) — as duas coisas juntas, não uma ou outra. HttpClient é injetado via
    // AddHttpClient (ver Program.cs), então BaseAddress e o certificado já vêm prontos.
    public class InterPagamentoGateway : IGatewayPagamentoSaque
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // Token de acesso é cacheado em campo estático (mesmo padrão de MercadoPagoConfig.AccessToken)
        // porque o endpoint de token do Inter tem rate limit de só 5 chamadas/minuto em produção — não
        // dá pra pedir um token novo a cada saque. Renovado 5 minutos antes de expirar (validade real:
        // 60 minutos) pra nunca correr o risco de usar um token vencido no meio de uma chamada.
        private static string? _tokenCache;
        private static DateTime _tokenExpiraEm = DateTime.MinValue;
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);

        public InterPagamentoGateway(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PixEnviado> EnviarPixAsync(EnvioPixSolicitado solicitacao)
        {
            var token = await ObterTokenAsync();

            var corpo = new
            {
                chavePix = new
                {
                    valor = solicitacao.Valor,
                    descricao = solicitacao.Descricao,
                    destinatario = new
                    {
                        tipo = "CHAVE",
                        chave = solicitacao.ChavePixDestino,
                    },
                },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/banking/v2/pix")
            {
                Content = JsonContent.Create(corpo),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("x-id-idempotente", solicitacao.IdIdempotente);

            var contaCorrente = _configuration["Inter:ContaCorrente"];
            if (!string.IsNullOrWhiteSpace(contaCorrente))
                request.Headers.Add("x-conta-corrente", contaCorrente);

            var response = await _httpClient.SendAsync(request);
            var corpoResposta = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Falha ao enviar Pix pelo Banco Inter ({(int)response.StatusCode}): {corpoResposta}");

            var resultado = JsonSerializer.Deserialize<InterPixResponse>(corpoResposta, JsonOpcoes)
                ?? throw new InvalidOperationException("Resposta vazia do Banco Inter ao enviar Pix.");

            return new PixEnviado(resultado.CodigoSolicitacao, resultado.TipoRetorno);
        }

        private async Task<string> ObterTokenAsync()
        {
            if (_tokenCache is not null && DateTime.UtcNow < _tokenExpiraEm)
                return _tokenCache;

            await _tokenLock.WaitAsync();
            try
            {
                // Outra chamada pode ter renovado o token enquanto esperávamos o lock.
                if (_tokenCache is not null && DateTime.UtcNow < _tokenExpiraEm)
                    return _tokenCache;

                var clientId = _configuration["Inter:ClientId"];
                var clientSecret = _configuration["Inter:ClientSecret"];

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                    throw new InvalidOperationException(
                        "Credenciais do Banco Inter não configuradas (Inter:ClientId / Inter:ClientSecret).");

                var corpo = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "pagamento-pix.write pagamento-pix.read",
                });

                var response = await _httpClient.PostAsync("/oauth/v2/token", corpo);
                var corpoResposta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        $"Falha ao autenticar com o Banco Inter ({(int)response.StatusCode}): {corpoResposta}");

                var tokenResponse = JsonSerializer.Deserialize<InterTokenResponse>(corpoResposta, JsonOpcoes)
                    ?? throw new InvalidOperationException("Resposta de token vazia do Banco Inter.");

                _tokenCache = tokenResponse.AccessToken;
                _tokenExpiraEm = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);

                return _tokenCache;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web);

        private sealed record InterTokenResponse(
            [property: JsonPropertyName("access_token")] string AccessToken,
            [property: JsonPropertyName("expires_in")] int ExpiresIn
        );

        private sealed record InterPixResponse(
            [property: JsonPropertyName("tipoRetorno")] string TipoRetorno,
            [property: JsonPropertyName("codigoSolicitacao")] string CodigoSolicitacao
        );
    }
}
