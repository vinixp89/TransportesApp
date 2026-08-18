using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    public class MotoristaService
    {
        private readonly IMotoristaRepository _motoristaRepository;

        public MotoristaService(IMotoristaRepository motoristaRepository)
        {
            _motoristaRepository = motoristaRepository;
        }

        public async Task<MotoristaResponse> CriarAsync(CriarMotoristaRequest request, Guid usuarioId)
        {
            var motorista = new Motorista(
                usuarioId: usuarioId,
                cnh: request.Cnh,
                placaVeiculo: request.PlacaVeiculo,
                modeloVeiculo: request.ModeloVeiculo
            );

            await _motoristaRepository.AdicionarAsync(motorista);

            return MapearParaResponse(motorista);
        }

        public async Task<MotoristaResponse?> ObterPorIdAsync(Guid id)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(id);

            return motorista is null ? null : MapearParaResponse(motorista);
        }

        public async Task<IEnumerable<MotoristaResponse>> ListarAsync()
        {
            var motoristas = await _motoristaRepository.ListarAsync();

            return motoristas.Select(MapearParaResponse);
        }

        public async Task<IEnumerable<MotoristaResponse>> ListarDisponiveisAsync()
        {
            var motoristas = await _motoristaRepository.ListarDisponiveisAsync();

            return motoristas.Select(MapearParaResponse);
        }

        private static MotoristaResponse MapearParaResponse(Motorista motorista)
        {
            return new MotoristaResponse(
                motorista.Id,
                motorista.UsuarioId,
                motorista.CNH,
                motorista.PlacaVeiculo,
                motorista.ModeloVeiculo,
                motorista.AvaliacaoMedia,
                motorista.DataCadastro
            );
        }
    }
}