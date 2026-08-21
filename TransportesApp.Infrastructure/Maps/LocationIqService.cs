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

        public async Task<(double DistanciaKm, double? DuracaoMinutos)> CalcularRotaAsync(
            double latitudeOrigem, double longitudeOrigem,
            double latitudeDestino, double longitudeDestino)
        {
            var apiKey = ObterApiKey();

            // O LocationIQ (baseado no OSRM) usa a ordem longitude,latitude — invertida em relação ao Google.
            var coordenadas =
                $"{FormatarNumero(longitudeOrigem)},{FormatarNumero(latitudeOrigem)};" +
                $"{FormatarNumero(longitudeDestino)},{FormatarNumero(latitudeDestino)}";

            var url = $"https://us1.locationiq.com/v1/matrix/driving/{coordenadas}" +
                      $"?key={apiKey}&sources=0&destinations=1&annotations=distance,duration";

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
            var duracaoSegundos = resposta?.Durations?.FirstOrDefault()?.FirstOrDefault();

            if (resposta is null || resposta.Code != "Ok" || distanciaMetros is null)
                throw new InvalidOperationException(
                    $"LocationIQ retornou um erro ao calcular a rota (status: {resposta?.Code ?? "sem resposta"}). " +
                    "Confira se as coordenadas informadas são válidas.");

            // A Matrix API retorna distância em metros e duração em segundos.
            var duracaoMinutos = duracaoSegundos is not null ? duracaoSegundos.Value / 60.0 : (double?)null;

            return (distanciaMetros.Value / 1000.0, duracaoMinutos);
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

        public async Task<IReadOnlyList<(string PlaceId, string Descricao)>> AutocompletarEnderecoAsync(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3)
                return Array.Empty<(string, string)>();

            var apiKey = ObterApiKey();
            var textoCodificado = Uri.EscapeDataString(texto);
            var url = $"https://us1.locationiq.com/v1/autocomplete?key={apiKey}&q={textoCodificado}" +
                      "&countrycodes=br&limit=5&format=json";

            List<LocationIqAutocompleteResult>? resposta;

            try
            {
                resposta = await _httpClient.GetFromJsonAsync<List<LocationIqAutocompleteResult>>(url, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Não foi possível contatar o serviço de mapas no momento.", ex);
            }

            if (resposta is null)
                return Array.Empty<(string, string)>();

            // O LocationIQ já devolve o endereço completo (com bairro/cidade/estado/coordenadas) direto
            // no autocomplete, sem uma etapa de "detalhes" separada como a do Google. Então o "PlaceId"
            // aqui não é um id de verdade — é o próprio resultado codificado, pra ObterDetalhesEnderecoAsync
            // conseguir decodificar sem precisar de outra chamada (nem de guardar estado entre requisições).
            return resposta
                .Where(r => !string.IsNullOrWhiteSpace(r.DisplayName))
                .Select(r => (CodificarComoPlaceId(r), r.DisplayName!))
                .ToList();
        }

        public Task<(string Logradouro, string? Numero, string? Bairro, string Cidade, string Estado, double Latitude, double Longitude)?> ObterDetalhesEnderecoAsync(string placeId)
        {
            var resultado = DecodificarPlaceId(placeId);

            if (resultado is null
                || !double.TryParse(resultado.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !double.TryParse(resultado.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                return Task.FromResult<(string, string?, string?, string, string, double, double)?>(null);
            }

            var endereco = resultado.Address;
            var logradouro = endereco?.Road ?? "";
            var numero = endereco?.HouseNumber;
            var bairro = endereco?.Suburb ?? endereco?.Neighbourhood;
            var cidade = endereco?.City ?? endereco?.Town ?? endereco?.Municipality ?? "";
            var estado = endereco?.State ?? "";

            return Task.FromResult<(string, string?, string?, string, string, double, double)?>(
                (logradouro, numero, bairro, cidade, estado, latitude, longitude));
        }

        private static string CodificarComoPlaceId(LocationIqAutocompleteResult resultado)
        {
            var json = JsonSerializer.Serialize(resultado);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        }

        private static LocationIqAutocompleteResult? DecodificarPlaceId(string placeId)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(placeId));
                return JsonSerializer.Deserialize<LocationIqAutocompleteResult>(json, JsonOptions);
            }
            catch
            {
                // PlaceId inválido/adulterado — trata igual a "não encontrado" em vez de derrubar a API.
                return null;
            }
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

        // Matriz [origem][destino], em segundos.
        public List<List<double?>>? Durations { get; set; }
    }

    // Modelo mínimo pra desserializar a resposta da Autocomplete API do LocationIQ (formato Nominatim).
    // Referência: https://docs.locationiq.com/reference/autocomplete
    internal class LocationIqAutocompleteResult
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        public string? Lat { get; set; }
        public string? Lon { get; set; }
        public LocationIqAddress? Address { get; set; }
    }

    internal class LocationIqAddress
    {
        public string? Road { get; set; }

        [JsonPropertyName("house_number")]
        public string? HouseNumber { get; set; }

        public string? Suburb { get; set; }
        public string? Neighbourhood { get; set; }
        public string? City { get; set; }
        public string? Town { get; set; }
        public string? Municipality { get; set; }
        public string? State { get; set; }
    }
}
