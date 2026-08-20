using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Infrastructure.Maps
{
    public class GoogleMapsService : IMapsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<double> CalcularDistanciaKmAsync(
            double latitudeOrigem, double longitudeOrigem,
            double latitudeDestino, double longitudeDestino)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Google Maps API Key não configurada (GoogleMaps:ApiKey nos User Secrets).");

            var origem = FormatarCoordenada(latitudeOrigem, longitudeOrigem);
            var destino = FormatarCoordenada(latitudeDestino, longitudeDestino);

            var url = "https://maps.googleapis.com/maps/api/distancematrix/json" +
                      $"?origins={origem}&destinations={destino}&units=metric&key={apiKey}";

            GoogleDistanceMatrixResponse? resposta;

            try
            {
                resposta = await _httpClient.GetFromJsonAsync<GoogleDistanceMatrixResponse>(url, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Não foi possível contatar o serviço de mapas no momento.", ex);
            }

            if (resposta is null || resposta.Status != "OK")
                throw new InvalidOperationException(
                    $"Google Maps retornou um erro ao calcular a rota (status: {resposta?.Status ?? "sem resposta"}" +
                    $"{(string.IsNullOrWhiteSpace(resposta?.ErrorMessage) ? "" : $" — {resposta!.ErrorMessage}")}).");

            var elemento = resposta.Rows?.FirstOrDefault()?.Elements?.FirstOrDefault();
            var distanciaMetros = elemento?.Distance?.Metros;

            if (elemento is null || elemento.Status != "OK" || distanciaMetros is null)
                throw new InvalidOperationException(
                    "Não foi possível calcular a distância entre a origem e o destino informados. Confira se os endereços/coordenadas são válidos.");

            // O Google retorna a distância em metros.
            return distanciaMetros.Value / 1000.0;
        }

        public async Task<(double Latitude, double Longitude)> GeocodificarAsync(string endereco)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Google Maps API Key não configurada (GoogleMaps:ApiKey nos User Secrets).");

            var enderecoCodificado = Uri.EscapeDataString(endereco);
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={enderecoCodificado}&key={apiKey}";

            GoogleGeocodeResponse? resposta;

            try
            {
                resposta = await _httpClient.GetFromJsonAsync<GoogleGeocodeResponse>(url, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Não foi possível contatar o serviço de mapas no momento.", ex);
            }

            var localizacao = resposta?.Results?.FirstOrDefault()?.Geometry?.Location;
            var latitude = localizacao?.Lat;
            var longitude = localizacao?.Lng;

            if (resposta is null || resposta.Status != "OK" || latitude is null || longitude is null)
                throw new InvalidOperationException(
                    $"Não foi possível localizar o endereço \"{endereco}\" no Google Maps (status: {resposta?.Status ?? "sem resposta"}" +
                    $"{(string.IsNullOrWhiteSpace(resposta?.ErrorMessage) ? "" : $" — {resposta!.ErrorMessage}")}). " +
                    "Confira se o endereço está completo e correto, ou informe latitude/longitude manualmente.");

            return (latitude.Value, longitude.Value);
        }

        private static string FormatarCoordenada(double latitude, double longitude)
            => $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}";
    }

    // Modelos mínimos pra desserializar a resposta da Distance Matrix API do Google.
    // Referência: https://developers.google.com/maps/documentation/distance-matrix/distance-matrix
    internal class GoogleDistanceMatrixResponse
    {
        public string? Status { get; set; }
        public List<GoogleDistanceMatrixRow>? Rows { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    internal class GoogleDistanceMatrixRow
    {
        public List<GoogleDistanceMatrixElement>? Elements { get; set; }
    }

    internal class GoogleDistanceMatrixElement
    {
        public string? Status { get; set; }
        public GoogleDistanceMatrixValue? Distance { get; set; }
        public GoogleDistanceMatrixValue? Duration { get; set; }
    }

    internal class GoogleDistanceMatrixValue
    {
        public string? Text { get; set; }

        // Metros (para Distance) ou segundos (para Duration). Renomeado de "Value" pra "Metros" pra não
        // colidir com o Nullable<T>.Value do C# — o Google chama esse campo de "value" no JSON.
        [JsonPropertyName("value")]
        public double? Metros { get; set; }
    }

    // Modelos mínimos pra desserializar a resposta da Geocoding API do Google.
    // Referência: https://developers.google.com/maps/documentation/geocoding/requests-geocoding
    internal class GoogleGeocodeResponse
    {
        public string? Status { get; set; }
        public List<GoogleGeocodeResult>? Results { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    internal class GoogleGeocodeResult
    {
        public GoogleGeocodeGeometry? Geometry { get; set; }
    }

    internal class GoogleGeocodeGeometry
    {
        public GoogleGeocodeLocation? Location { get; set; }
    }

    internal class GoogleGeocodeLocation
    {
        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lng")]
        public double? Lng { get; set; }
    }
}