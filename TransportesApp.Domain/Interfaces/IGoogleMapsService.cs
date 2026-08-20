namespace TransportesApp.Domain.Interfaces
{
    public interface IGoogleMapsService
    {
        Task<double> CalcularDistanciaKmAsync(
            double latitudeOrigem, double longitudeOrigem,
            double latitudeDestino, double longitudeDestino);

        // Transforma um endereço em texto (ex: "Rua Ana Carla, 110, Nova Iguaçu, RJ") em coordenadas,
        // pra quando o cliente não sabe/não informa latitude e longitude.
        Task<(double Latitude, double Longitude)> GeocodificarAsync(string endereco);
    }
}
