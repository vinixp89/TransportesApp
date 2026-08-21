namespace TransportesApp.Domain.Interfaces
{
    public interface IMapsService
    {
        // DuracaoMinutos vem null quando o provedor não retornou essa informação (ex: LocationIQ
        // sem a anotação de duração habilitada) — quem chama deve tratar como "tempo não disponível".
        Task<(double DistanciaKm, double? DuracaoMinutos)> CalcularRotaAsync(
            double latitudeOrigem, double longitudeOrigem,
            double latitudeDestino, double longitudeDestino);

        // Transforma um endereço em texto (ex: "Rua Ana Carla, 110, Nova Iguaçu, RJ") em coordenadas,
        // pra quando o cliente não sabe/não informa latitude e longitude.
        // CorrespondenciaParcial vem true quando o provedor não teve certeza total do endereço
        // (ex: rua/bairro digitado com erro, mas achou algo parecido) — vale avisar o cliente.
        Task<(double Latitude, double Longitude, bool CorrespondenciaParcial)> GeocodificarAsync(string endereco);

        // Sugestões de endereço enquanto o usuário digita (autocomplete). PlaceId é opaco — cada
        // provedor decide o que colocar ali, só serve pra passar de volta em ObterDetalhesEnderecoAsync.
        Task<IReadOnlyList<(string PlaceId, string Descricao)>> AutocompletarEnderecoAsync(string texto);

        // Detalha uma sugestão escolhida em campos estruturados (logradouro/número/bairro/cidade/estado)
        // prontos pra virar um EnderecoRequest — Numero pode vir nulo quando o local não tem número
        // (ex: uma praça), quem chama decide o que fazer nesse caso. Retorna null se o PlaceId for inválido.
        Task<(string Logradouro, string? Numero, string? Bairro, string Cidade, string Estado, double Latitude, double Longitude)?> ObterDetalhesEnderecoAsync(string placeId);
    }
}
