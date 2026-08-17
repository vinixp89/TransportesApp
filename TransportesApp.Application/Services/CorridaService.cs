using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Application.Services
{
    public class CorridaService
    {
        private readonly ICorridaRepository _corridaRepository;

        public CorridaService(ICorridaRepository corridaRepository)
        {
            _corridaRepository = corridaRepository;
        }

        public async Task<CorridaResponse> CriarAsync(CriarCorridasRequest request)
        {
            var origem = MapearEndereco(request.Origem);
            var destino = MapearEndereco(request.Destino);

            var faixa = FaixaDistancia.ClassificarPorDistancia(request.DistanciaEstimadaKm);

            var corrida = new Corrida(
                clienteId: request.ClienteId,
                origem: origem,
                destino: destino,
                distanciaEstimadaKm: request.DistanciaEstimadaKm,
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

        private static Endereco MapearEndereco(EnderecoRequest request)
        {
            return new Endereco(
                logradouro: request.Logradouro,
                numero: request.Numero,
                bairro: request.Bairro,
                cidade: request.Cidade,
                estado: request.Estado,
                latitude: request.Latitude,
                longitude: request.Longitude,
                complemento: request.Complemento
            );
        }

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