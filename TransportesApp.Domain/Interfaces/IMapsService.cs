namespace TransportesApp.Domain.Interfaces
{
    public interface IMapsService
    {
        Task<double> CalcularDistanciaKmAsync(
            double latitudeOrigem, double longitudeOrigem,
            double latitudeDestino, double longitudeDestino);

        // Transforma um endereço em texto (ex: "Rua Ana Carla, 110, Nova Iguaçu, RJ") em coordenadas,
        // pra quando o cliente não sabe/não informa latitude e longitude.
        // CorrespondenciaParcial vem true quando o provedor não teve certeza total do endereço
        // (ex: rua/bairro digitado com erro, mas achou algo parecido) — vale avisar o cliente.
        Task<(double Latitude, double Longitude, bool CorrespondenciaParcial)> GeocodificarAsync(string endereco);
    }
}
