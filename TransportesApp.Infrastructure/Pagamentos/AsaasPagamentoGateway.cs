using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using TransportesApp.Domain.Common;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Pagamentos
{
    // Implementação de IGatewayPagamentoSaque usando a API do Asaas (https://docs.asaas.com) — em uso
    // no lugar do Banco Inter (ver InterPagamentoGateway, mantido no projeto mas fora da injeção de
    // dependência) porque a conta do Inter ficou impedida de criar uma nova aplicação (negada/revogada,
    // com bloqueio de alguns meses pra tentar de novo).
    //
    // Autenticação é bem mais simples que a do Inter: um Access Token fixo no header "access_token",
    // sem OAuth e sem certificado mTLS — gerado em Configurações > Integrações > API dentro da conta
    // Asaas (produção e sandbox têm cada uma a sua própria chave).
    //
    // IMPORTANTE, verificar no sandbox antes de confiar isso em produção: não há confirmação de que
    // o endpoint de transferência do Asaas trata uma chamada repetida (retry de rede, duplo clique)
    // como idempotente — diferente do Inter, que tinha o header x-id-idempotente dedicado pra isso.
    // "externalReference" abaixo serve pra conseguir rastrear/conciliar manualmente qual solicitação
    // gerou qual transferência, mas não é garantia de que o Asaas não processa a mesma chamada duas
    // vezes se ela for reenviada.
    public class AsaasPagamentoGateway : IGatewayPagamentoSaque
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AsaasPagamentoGateway(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PixEnviado> EnviarPixAsync(EnvioPixSolicitado solicitacao)
        {
            var apiKey = _configuration["Asaas:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Credenciais do Asaas não configuradas (Asaas:ApiKey).");

            var corpo = new
            {
                value = solicitacao.Valor,
                pixAddressKey = solicitacao.ChavePixDestino,
                pixAddressKeyType = DetectarTipoChavePix(solicitacao.ChavePixDestino),
                description = solicitacao.Descricao,
                externalReference = solicitacao.IdIdempotente,
            };

            // Sem barra inicial de propósito — com "/transfers" o .NET descarta o "/v3/" (ou
            // "/api/v3/") que já vem na BaseAddress, porque uma barra inicial é tratada como caminho
            // absoluto a partir da raiz do host, não como complemento da BaseAddress.
            using var request = new HttpRequestMessage(HttpMethod.Post, "transfers")
            {
                Content = JsonContent.Create(corpo, options: JsonOpcoes),
            };
            request.Headers.Add("access_token", apiKey);
            // O Asaas recusa qualquer chamada sem User-Agent (erro "user_agent_not_informed").
            request.Headers.UserAgent.ParseAdd("VaiNaBoa-Backend/1.0");

            var response = await _httpClient.SendAsync(request);
            var corpoResposta = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Falha ao enviar Pix pelo Asaas ({(int)response.StatusCode}): {corpoResposta}");

            var resultado = JsonSerializer.Deserialize<AsaasTransferResponse>(corpoResposta, JsonOpcoes)
                ?? throw new InvalidOperationException("Resposta vazia do Asaas ao enviar Pix.");

            return new PixEnviado(resultado.Id, resultado.Status);
        }

        // O sistema só guarda a chave Pix em si (não o tipo dela), mas o Asaas exige informar se é
        // CPF, CNPJ, EMAIL, PHONE ou EVP (chave aleatória) — então deduzimos pelo formato. CPF e
        // telefone com DDD têm os dois 11 dígitos, então usamos o dígito verificador do CPF pra
        // desempatar; mesmo assim, um telefone que por coincidência "valide" como CPF seria
        // classificado errado — risco baixo, mas real, de um saque cair pra uma chave diferente da
        // pretendida. Se isso for um problema na prática, o jeito certo é passar a pedir o tipo da
        // chave explicitamente no formulário de saque em vez de adivinhar.
        private static string DetectarTipoChavePix(string chave)
        {
            var normalizada = chave.Trim();

            if (normalizada.Contains('@'))
                return "EMAIL";

            if (Guid.TryParse(normalizada, out _))
                return "EVP";

            var apenasDigitos = new string(normalizada.Where(char.IsDigit).ToArray());

            if (apenasDigitos.Length == 14)
                return "CNPJ";

            if (apenasDigitos.Length == 11)
                return CpfValidator.EhValido(apenasDigitos) ? "CPF" : "PHONE";

            return "PHONE";
        }

        private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web);

        private sealed record AsaasTransferResponse(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("status")] string Status
        );
    }
}
