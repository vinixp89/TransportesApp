using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Fica entre o controller e o IMapsService — existe só pra não expor a chave da API de mapas
    // pro navegador: o front chama a nossa API, a nossa API chama o Google (ou LocationIQ) com a
    // chave guardada no servidor.
    public class EnderecoAutocompleteService
    {
        private readonly IMapsService _mapsService;

        public EnderecoAutocompleteService(IMapsService mapsService)
        {
            _mapsService = mapsService;
        }

        public async Task<IReadOnlyList<SugestaoEnderecoResponse>> AutocompletarAsync(string texto)
        {
            var sugestoes = await _mapsService.AutocompletarEnderecoAsync(texto);

            return sugestoes.Select(s => new SugestaoEnderecoResponse(s.PlaceId, s.Descricao)).ToList();
        }

        public async Task<DetalhesEnderecoResponse?> ObterDetalhesAsync(string placeId)
        {
            var detalhes = await _mapsService.ObterDetalhesEnderecoAsync(placeId);

            if (detalhes is null)
                return null;

            return new DetalhesEnderecoResponse(
                detalhes.Value.Logradouro,
                detalhes.Value.Numero,
                detalhes.Value.Bairro,
                detalhes.Value.Cidade,
                detalhes.Value.Estado,
                detalhes.Value.Latitude,
                detalhes.Value.Longitude
            );
        }
    }
}
