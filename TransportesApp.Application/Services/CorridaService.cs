using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class CorridaService
    {
        private readonly ICorridaRepository _corridaRepository;
        private readonly IMapsService _mapsService;

        public CorridaService(ICorridaRepository corridaRepository, IMapsService mapsService)
        {
            _corridaRepository = corridaRepository;
            _mapsService = mapsService;
        }

        public async Task<CorridaResponse> CriarAsync(CriarCorridasRequest request, Guid clienteId)
        {
            var origem = await ResolverEnderecoAsync(request.Origem);
            var destino = await ResolverEnderecoAsync(request.Destino);

            var distanciaEstimadaKm = await _mapsService.CalcularDistanciaKmAsync(
                origem.Latitude, origem.Longitude, destino.Latitude, destino.Longitude);

            var faixa = FaixaDistancia.ClassificarPorDistancia(distanciaEstimadaKm);

            var corrida = new Corrida(
                clienteId: clienteId,
                origem: origem,
                destino: destino,
                distanciaEstimadaKm: distanciaEstimadaKm,
                faixa: faixa,
                tipoConsumo: request.TipoConsumo,
                pacoteCorridasId: request.PacoteCorridasId
            );

            await _corridaRepository.AdicionarAsync(corrida);

            return MapearParaResponse(corrida);
        }

        public async Task<CorridaResponse?> ObterPorIdAsync(Guid id)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(id);

            return corrida is null ? null : MapearParaResponse(corrida);
        }

        public async Task<IEnumerable<CorridaResponse>> ListarAsync()
        {
            var corridas = await _corridaRepository.ListarAsync();

            return corridas.Select(MapearParaResponse);
        }

        public async Task<CorridaResponse?> AtribuirMotoristaAsync(Guid corridaId, Guid motoristaId)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            corrida.AtribuirMotorista(motoristaId);

            await _corridaRepository.AtualizarAsync(corrida);

            return MapearParaResponse(corrida);
        }

        public async Task<CorridaResponse?> IniciarViagemAsync(Guid corridaId)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            corrida.IniciarViagem();

            await _corridaRepository.AtualizarAsync(corrida);

            return MapearParaResponse(corrida);
        }

        public async Task<FinalizarCorridaResponse?> FinalizarAsync(Guid corridaId, FinalizarCorridaRequest request)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            var estourouFaixa = corrida.FinalizarCorrida(request.DistanciaReal);

            await _corridaRepository.AtualizarAsync(corrida);

            return new FinalizarCorridaResponse(estourouFaixa, MapearParaResponse(corrida));
        }

        public async Task<CorridaResponse?> CancelarAsync(Guid corridaId)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(corridaId);

            if (corrida is null)
                return null;

            corrida.Cancelar();

            await _corridaRepository.AtualizarAsync(corrida);

            return MapearParaResponse(corrida);
        }

        // Sempre geocodifica o endereço em texto via Google Maps — o cliente não informa lat/long.
        private async Task<Endereco> ResolverEnderecoAsync(EnderecoRequest request)
        {
            var enderecoTexto = FormatarEnderecoParaGeocodificacao(request);
            var (latitude, longitude) = await _mapsService.GeocodificarAsync(enderecoTexto);

            return new Endereco(
                logradouro: request.Logradouro,
                numero: request.Numero,
                bairro: request.Bairro,
                cidade: request.Cidade,
                estado: request.Estado,
                latitude: latitude,
                longitude: longitude,
                complemento: request.Complemento
            );
        }

        private static string FormatarEnderecoParaGeocodificacao(EnderecoRequest request)
            => $"{request.Logradouro}, {request.Numero}, {request.Bairro}, {request.Cidade}, {request.Estado}, Brasil";

        private static EnderecoResponse MapearEnderecoResponse(Endereco endereco)
        {
            return new EnderecoResponse(
                endereco.Logradouro,
                endereco.Numero,
                endereco.Bairro,
                endereco.Cidade,
                endereco.Estado,
                endereco.Latitude,
                endereco.Longitude,
                endereco.Complemento
            );
        }

        private static CorridaResponse MapearParaResponse(Corrida corrida)
        {
            return new CorridaResponse(
                corrida.Id,
                corrida.ClienteId,
                corrida.MotoristaId,
                MapearEnderecoResponse(corrida.Origem),
                MapearEnderecoResponse(corrida.Destino),
                corrida.DistanciaEstimadaKm,
                corrida.DistanciaRealKm,
                corrida.FaixaContratada,
                corrida.TipoConsumo,
                corrida.Status,
                corrida.DataSolicitacao
            );
        }
    }
}