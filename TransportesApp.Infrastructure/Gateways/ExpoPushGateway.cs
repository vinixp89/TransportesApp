using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Gateways
{
    // Envia push de verdade (chega com o app fechado) via serviço do Expo, que repassa pro FCM
    // (Android) usando as credenciais do Firebase configuradas no projeto EAS de cada app — ver
    // google-services.json nos repositórios mobile e `eas credentials`. Sem custo, sem servidor
    // próprio. Doc oficial: https://docs.expo.dev/push-notifications/sending-notifications/#http2-api.
    public class ExpoPushGateway : IExpoPushGateway
    {
        private const int TamanhoLote = 100;

        private readonly HttpClient _httpClient;

        public ExpoPushGateway(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<string>> EnviarAsync(IReadOnlyList<string> tokens, string titulo, string mensagem)
        {
            var tokensInvalidos = new List<string>();

            foreach (var lote in tokens.Chunk(TamanhoLote))
                tokensInvalidos.AddRange(await EnviarLoteAsync(lote, titulo, mensagem));

            return tokensInvalidos;
        }

        // O Expo só aceita, por requisição, tokens de um único projeto (app) — um token do app
        // Motorista registrado como Cliente (ex: mesma conta logada nos dois apps) derrubava o
        // broadcast inteiro com PUSH_TOO_MANY_EXPERIENCE_IDS. Quando isso acontece, a própria
        // resposta do Expo diz quais tokens pertencem a qual projeto (details), então reenviamos um
        // grupo por vez — cada grupo já é de um projeto só, então não cai nesse erro de novo.
        private async Task<IReadOnlyList<string>> EnviarLoteAsync(string[] lote, string titulo, string mensagem)
        {
            var mensagens = lote.Select(token => new ExpoMensagem(token, titulo, mensagem, "default")).ToArray();

            using var response = await _httpClient.PostAsJsonAsync("/--/api/v2/push/send", mensagens, JsonOpcoes);
            var corpoResposta = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var grupos = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? ExtrairGruposPorProjeto(corpoResposta)
                    : null;

                if (grupos is { Count: > 1 })
                {
                    var invalidosDosGrupos = new List<string>();

                    foreach (var grupo in grupos)
                        invalidosDosGrupos.AddRange(await EnviarLoteAsync(grupo, titulo, mensagem));

                    return invalidosDosGrupos;
                }

                throw new InvalidOperationException($"Falha ao enviar push pelo Expo ({(int)response.StatusCode}): {corpoResposta}");
            }

            var resultado = JsonSerializer.Deserialize<ExpoRespostaEnvio>(corpoResposta, JsonOpcoes);
            var tokensInvalidos = new List<string>();

            if (resultado?.Data is null)
                return tokensInvalidos;

            for (var i = 0; i < resultado.Data.Length && i < lote.Length; i++)
            {
                if (resultado.Data[i].Details?.Error == "DeviceNotRegistered")
                    tokensInvalidos.Add(lote[i]);
            }

            return tokensInvalidos;
        }

        // Lê o "details" do erro PUSH_TOO_MANY_EXPERIENCE_IDS: { "@dono/projeto": ["token", ...], ... }.
        private static List<string[]>? ExtrairGruposPorProjeto(string corpoResposta)
        {
            try
            {
                var resposta = JsonSerializer.Deserialize<ExpoRespostaErro>(corpoResposta, JsonOpcoes);
                var erro = resposta?.Errors?.FirstOrDefault(e => e.Code == "PUSH_TOO_MANY_EXPERIENCE_IDS");

                return erro?.Details?.Values.Where(tokens => tokens.Length > 0).ToList();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web);

        private sealed record ExpoMensagem(string To, string Title, string Body, string Sound);

        private sealed record ExpoRespostaEnvio(
            [property: JsonPropertyName("data")] ExpoTicket[]? Data
        );

        private sealed record ExpoRespostaErro(
            [property: JsonPropertyName("errors")] ExpoErro[]? Errors
        );

        private sealed record ExpoErro(
            [property: JsonPropertyName("code")] string? Code,
            [property: JsonPropertyName("details")] Dictionary<string, string[]>? Details
        );

        private sealed record ExpoTicket(
            [property: JsonPropertyName("status")] string Status,
            [property: JsonPropertyName("details")] ExpoTicketDetalhes? Details
        );

        private sealed record ExpoTicketDetalhes(
            [property: JsonPropertyName("error")] string? Error
        );
    }
}
