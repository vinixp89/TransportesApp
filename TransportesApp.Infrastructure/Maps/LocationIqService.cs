using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Maps
{
    // Alternativa gratuita ao Google Maps (não exige cartão de crédito), baseada em OpenStreetMap.
    // Documentação: https://docs.locationiq.com
    public class LocationIqService : IMapsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LocationIqService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<double> CalcularDistanciaKmAsync(
            double latitudeOrigem, double longitudeOrigem,
            double latitudeDestino, double longitudeDestino)
        {
            var apiKey = ObterApiKey();

            // O LocationIQ (baseado no OSRM) usa a ordem longitude,latitude — invertida em relação ao Google.
            var coordenadas =
                $"{FormatarNumero(longitudeOrigem)},{FormatarNumero(latitudeOrigem)};" +
                $"{FormatarNumero(longitudeDestino)},{FormatarNumero(latitudeDestino)}";

            var url = $"https://us1.locationiq.com/v1/matrix/driving/{coordenadas}" +
                      $"?key={apiKey}&sources=0&destinations=1&annotations=distance";

            LocationIqMatrixResponse? resposta;

            try
            {
                resposta = await _httpClient.GetFromJsonAsync<LocationIqMatrixResponse>(url, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Não foi possível contatar o serviço de mapas no momento.", ex);
            }

            var distanciaMetros = resposta?.Distances?.FirstOrDefault()?.FirstOrDefault();

            if (resposta is null || resposta.Code != "Ok" || distanciaMetros is null)
                throw new InvalidOperationException(
                    $"LocationIQ retornou um erro ao calcular a rota (status: {resposta?.Code ?? "sem resposta"}). " +
                    "Confira se as coordenadas informadas são válidas.");

            // A Matrix API retorna a distância em metros.
            return distanciaMetros.Value / 1000.0;
        }

        public async Task<(double Latitude, double Longitude, bool CorrespondenciaParcial)> GeocodificarAsync(string endereco)
        {
            var apiKey = ObterApiKey();

            var enderecoCodificado = Uri.EscapeDataString(endereco);
            var url = $"https://us1.locationiq.com/v1/search?key={apiKey}&q={enderecoCodificado}&format=json&limit=1";

            List<LocationIqSearchResult>? resposta;

            try
            {
                resposta = await _httpClient.GetFromJsonAsync<List<LocationIqSearchResult>>(url, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Não foi possível contatar o serviço de mapas no momento.", ex);
            }

            var resultado = resposta?.FirstOrDefault();

            // O LocationIQ (Nominatim) devolve lat/lon como texto, não como número — precisa converter.
            if (resultado is null
                || !double.TryParse(resultado.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !double.TryParse(resultado.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                throw new InvalidOperationException(
                    $"Não foi possível localizar o endereço \"{endereco}\" no LocationIQ. " +
                    "Confira se o endereço está completo e correto, ou informe latitude/longitude manualmente.");
            }

            // O LocationIQ/Nominatim não tem um indicador equivalente de "correspondência parcial",
            // então sempre retorna false aqui.
            return (latitude, longitude, false);
        }

        private string ObterApiKey()
        {
            var apiKey = _configuration["LocationIq:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "LocationIQ API Key não configurada (LocationIq:ApiKey nos User Secrets).");

            return apiKey;
        }

        private static string FormatarNumero(double valor) => valor.ToString(CultureInfo.InvariantCulture);
    }

    // Modelo mínimo pra desserializar a resposta da Search API (geocoding) do LocationIQ.
    // Referência: https://docs.locationiq.com/reference/search
    internal class LocationIqSearchResult
    {
        [JsonPropertyName("lat")]
        public string? Lat { get; set; }

        [JsonPropertyName("lon")]
        public string? Lon { get; set; }
    }

    // Modelo mínimo pra desserializar a resposta da Matrix API (distância) do LocationIQ, formato OSRM.
    // Referência: https://docs.locationiq.com/reference/matrix
    internal class LocationIqMatrixResponse
    {
        public string? Code { get; set; }

        // Matriz [origem][destino], em metros.
        public List<List<double?>>? Distances { get; set; }
    }
}
