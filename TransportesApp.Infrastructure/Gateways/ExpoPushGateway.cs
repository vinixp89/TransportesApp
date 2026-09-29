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
            {
                var mensagens = lote.Select(token => new ExpoMensagem(token, titulo, mensagem, "default")).ToArray();

                using var response = await _httpClient.PostAsJsonAsync("/--/api/v2/push/send", mensagens, JsonOpcoes);
                var corpoResposta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Falha ao enviar push pelo Expo ({(int)response.StatusCode}): {corpoResposta}");

                var resultado = JsonSerializer.Deserialize<ExpoRespostaEnvio>(corpoResposta, JsonOpcoes);

                if (resultado?.Data is null)
                    continue;

                for (var i = 0; i < resultado.Data.Length && i < lote.Length; i++)
                {
                    if (resultado.Data[i].Details?.Error == "DeviceNotRegistered")
                        tokensInvalidos.Add(lote[i]);
                }
            }

            return tokensInvalidos;
        }

        private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web);

        private sealed record ExpoMensagem(string To, string Title, string Body, string Sound);

        private sealed record ExpoRespostaEnvio(
            [property: JsonPropertyName("data")] ExpoTicket[]? Data
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
