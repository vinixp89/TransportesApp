using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    public class AvisoService
    {
        private readonly IAvisoRepository _avisoRepository;

        public AvisoService(IAvisoRepository avisoRepository)
        {
            _avisoRepository = avisoRepository;
        }

        public async Task<AvisoResponse?> ObterAtivoAsync()
        {
            var aviso = await _avisoRepository.ObterAtivoAsync(DateTime.UtcNow);
            return aviso is null ? null : Mapear(aviso);
        }

        public async Task<IEnumerable<AvisoResponse>> ListarAsync()
        {
            var avisos = await _avisoRepository.ListarAsync();
            return avisos.Select(Mapear);
        }

        public async Task<AvisoResponse> CriarAsync(CriarAvisoRequest request)
        {
            var aviso = new Aviso(
                request.Titulo,
                request.Texto,
                request.CorFundoHex,
                request.TextoBotao,
                request.TelaDestino,
                request.DataInicio,
                request.DataFim);

            await _avisoRepository.AdicionarAsync(aviso);
            return Mapear(aviso);
        }

        public async Task<AvisoResponse> AtualizarAsync(Guid id, AtualizarAvisoRequest request)
        {
            var aviso = await _avisoRepository.ObterPorIdAsync(id)
                ?? throw new KeyNotFoundException("Aviso não encontrado.");

            aviso.Atualizar(
                request.Titulo,
                request.Texto,
                request.CorFundoHex,
                request.TextoBotao,
                request.TelaDestino,
                request.DataInicio,
                request.DataFim);

            if (request.Ativo)
                aviso.Ativar();
            else
                aviso.Desativar();

            await _avisoRepository.AtualizarAsync(aviso);
            return Mapear(aviso);
        }

        public async Task ExcluirAsync(Guid id)
        {
            var aviso = await _avisoRepository.ObterPorIdAsync(id)
                ?? throw new KeyNotFoundException("Aviso não encontrado.");

            await _avisoRepository.RemoverAsync(aviso);
        }

        private static AvisoResponse Mapear(Aviso aviso) => new(
            aviso.Id,
            aviso.Titulo,
            aviso.Texto,
            aviso.CorFundoHex,
            aviso.TextoBotao,
            aviso.TelaDestino,
            aviso.DataInicio,
            aviso.DataFim,
            aviso.Ativo);
    }
}
