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

        public async Task<(double DistanciaKm, double? DuracaoMinutos)> CalcularRotaAsync(
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
            var duracaoSegundos = elemento?.Duration?.Metros;

            if (elemento is null || elemento.Status != "OK" || distanciaMetros is null)
                throw new InvalidOperationException(
                    "Não foi possível calcular a distância entre a origem e o destino informados. Confira se os endereços/coordenadas são válidos.");

            // O Google retorna distância em metros e duração em segundos.
            var duracaoMinutos = duracaoSegundos is not null ? duracaoSegundos.Value / 60.0 : (double?)null;

            return (distanciaMetros.Value / 1000.0, duracaoMinutos);
        }

        public async Task<(double Latitude, double Longitude, bool CorrespondenciaParcial)> GeocodificarAsync(string endereco)
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

            var resultado = resposta?.Results?.FirstOrDefault();
            var localizacao = resultado?.Geometry?.Location;
            var latitude = localizacao?.Lat;
            var longitude = localizacao?.Lng;

            if (resposta is null || resposta.Status != "OK" || latitude is null || longitude is null)
                throw new InvalidOperationException(
                    $"Não foi possível localizar o endereço \"{endereco}\" no Google Maps (status: {resposta?.Status ?? "sem resposta"}" +
                    $"{(string.IsNullOrWhiteSpace(resposta?.ErrorMessage) ? "" : $" — {resposta!.ErrorMessage}")}). " +
                    "Confira se o endereço está completo e correto, ou informe latitude/longitude manualmente.");

            // partial_match = true quando o Google não teve certeza total do endereço digitado
            // (ex: rua/bairro com erro de digitação, mas achou algo parecido).
            var correspondenciaParcial = resultado!.PartialMatch ?? false;

            return (latitude.Value, longitude.Value, correspondenciaParcial);
        }

        public async Task<IReadOnlyList<(string PlaceId, string Descricao)>> AutocompletarEnderecoAsync(string texto)
        {
            // Menos de 3 caracteres dá sugestões ruins/genéricas demais — nem vale chamar a API.
            if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3)
                return Array.Empty<(string, string)>();

            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Google Maps API Key não configurada (GoogleMaps:ApiKey nos User Secrets).");

            var textoCodificado = Uri.EscapeDataString(texto);
            var url = "https://maps.googleapis.com/maps/api/place/autocomplete/json" +
                      $"?input={textoCodificado}&key={apiKey}&language=pt-BR&components=country:br";

            GooglePlaceAutocompleteResponse? resposta;

            try
            {
                resposta = await _httpClient.GetFromJsonAsync<GooglePlaceAutocompleteResponse>(url, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Não foi possível contatar o serviço de mapas no momento.", ex);
            }

            // ZERO_RESULTS não é erro — só quer dizer que nada bateu com o texto digitado até agora.
            if (resposta is null || (resposta.Status != "OK" && resposta.Status != "ZERO_RESULTS"))
                throw new InvalidOperationException(
                    $"Google Maps retornou um erro ao buscar sugestões de endereço (status: {resposta?.Status ?? "sem resposta"}" +
                    $"{(string.IsNullOrWhiteSpace(resposta?.ErrorMessage) ? "" : $" — {resposta!.ErrorMessage}")}).");

            return resposta.Predictions?
                .Where(p => !string.IsNullOrWhiteSpace(p.PlaceId) && !string.IsNullOrWhiteSpace(p.Description))
                .Select(p => (p.PlaceId!, p.Description!))
                .ToList()
                ?? new List<(string, string)>();
        }

        public async Task<(string Logradouro, string? Numero, string? Bairro, string Cidade, string Estado, double Latitude, double Longitude)?> ObterDetalhesEnderecoAsync(string placeId)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Google Maps API Key não configurada (GoogleMaps:ApiKey nos User Secrets).");

            var placeIdCodificado = Uri.EscapeDataString(placeId);
            var url = "https://maps.googleapis.com/maps/api/place/details/json" +
                      $"?place_id={placeIdCodificado}&key={apiKey}&language=pt-BR&fields=address_component,geometry";

            GooglePlaceDetailsResponse? resposta;

            try
            {
                resposta = await _httpClient.GetFromJsonAsync<GooglePlaceDetailsResponse>(url, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Não foi possível contatar o serviço de mapas no momento.", ex);
            }

            if (resposta?.Result is null || resposta.Status != "OK")
                return null;

            var latitude = resposta.Result.Geometry?.Location?.Lat;
            var longitude = resposta.Result.Geometry?.Location?.Lng;

            if (latitude is null || longitude is null)
                return null;

            var componentes = resposta.Result.AddressComponents ?? new List<GooglePlaceAddressComponent>();

            string? BuscarComponente(params string[] tipos) =>
                componentes.FirstOrDefault(c => c.Types is not null && c.Types.Any(tipos.Contains))?.LongName;

            var logradouro = BuscarComponente("route") ?? "";
            var numero = BuscarComponente("street_number");
            var bairro = BuscarComponente("sublocality", "sublocality_level_1", "neighborhood");
            var cidade = BuscarComponente("administrative_area_level_2", "locality") ?? "";
            var estado = componentes
                .FirstOrDefault(c => c.Types is not null && c.Types.Contains("administrative_area_level_1"))
                ?.ShortName ?? "";

            return (logradouro, numero, bairro, cidade, estado, latitude.Value, longitude.Value);
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

        [JsonPropertyName("partial_match")]
        public bool? PartialMatch { get; set; }
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

    // Modelos mínimos pra desserializar a resposta da Places Autocomplete API do Google.
    // Referência: https://developers.google.com/maps/documentation/places/web-service/autocomplete
    internal class GooglePlaceAutocompleteResponse
    {
        public string? Status { get; set; }
        public List<GooglePlacePrediction>? Predictions { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    internal class GooglePlacePrediction
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }

        public string? Description { get; set; }
    }

    // Modelos mínimos pra desserializar a resposta da Place Details API do Google.
    // Referência: https://developers.google.com/maps/documentation/places/web-service/details
    internal class GooglePlaceDetailsResponse
    {
        public string? Status { get; set; }
        public GooglePlaceDetailsResult? Result { get; set; }
    }

    internal class GooglePlaceDetailsResult
    {
        [JsonPropertyName("address_components")]
        public List<GooglePlaceAddressComponent>? AddressComponents { get; set; }

        public GoogleGeocodeGeometry? Geometry { get; set; }
    }

    internal class GooglePlaceAddressComponent
    {
        [JsonPropertyName("long_name")]
        public string? LongName { get; set; }

        [JsonPropertyName("short_name")]
        public string? ShortName { get; set; }

        public List<string>? Types { get; set; }
    }
}
