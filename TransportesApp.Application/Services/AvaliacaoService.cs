using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Avaliação de corrida finalizada — quem decide QUANDO é permitido avaliar (status da corrida) e
    // QUEM pode avaliar (participante da corrida) é o CorridasController, mesmo padrão do
    // MensagemChatService: aqui só existe a regra de persistência + recálculo de média, sem repetir
    // nenhuma checagem de autorização.
    public class AvaliacaoService
    {
        private readonly IAvaliacaoRepository _avaliacaoRepository;
        private readonly IMotoristaRepository _motoristaRepository;
        private readonly IClienteRepository _clienteRepository;

        public AvaliacaoService(
            IAvaliacaoRepository avaliacaoRepository,
            IMotoristaRepository motoristaRepository,
            IClienteRepository clienteRepository)
        {
            _avaliacaoRepository = avaliacaoRepository;
            _motoristaRepository = motoristaRepository;
            _clienteRepository = clienteRepository;
        }

        public async Task<AvaliacaoResponse> AvaliarAsync(Guid corridaId, TipoUsuario autorTipo, Guid avaliadoId, int nota, string? comentario)
        {
            var existente = await _avaliacaoRepository.ObterPorCorridaEAutorAsync(corridaId, autorTipo);
            if (existente is not null)
                throw new InvalidOperationException("Você já avaliou essa corrida.");

            var avaliacao = new Avaliacao(corridaId, autorTipo, avaliadoId, nota, comentario);
            await _avaliacaoRepository.AdicionarAsync(avaliacao);

            await AtualizarMediaAsync(autorTipo, avaliadoId);

            return MapearParaResponse(avaliacao);
        }

        public async Task<IEnumerable<AvaliacaoResponse>> ListarPorCorridaAsync(Guid corridaId)
        {
            var avaliacoes = await _avaliacaoRepository.ListarPorCorridaAsync(corridaId);
            return avaliacoes.Select(MapearParaResponse);
        }

        // AutorTipo aqui é de quem ESCREVEU a avaliação recém-criada — então quem teve a média
        // recalculada é o AVALIADO, do tipo oposto (Cliente avalia Motorista e vice-versa).
        private async Task AtualizarMediaAsync(TipoUsuario autorTipo, Guid avaliadoId)
        {
            var media = await _avaliacaoRepository.ObterMediaAsync(avaliadoId);

            if (autorTipo == TipoUsuario.Cliente)
            {
                var motorista = await _motoristaRepository.ObterPorIdAsync(avaliadoId);
                if (motorista is null)
                    return;

                motorista.DefinirAvaliacaoMedia(media ?? 5.0);
                await _motoristaRepository.AtualizarAsync(motorista);
            }
            else
            {
                var cliente = await _clienteRepository.ObterPorIdAsync(avaliadoId);
                if (cliente is null)
                    return;

                cliente.DefinirAvaliacaoMedia(media);
                await _clienteRepository.AtualizarAsync(cliente);
            }
        }

        private static AvaliacaoResponse MapearParaResponse(Avaliacao avaliacao)
            => new(avaliacao.Id, avaliacao.CorridaId, avaliacao.AutorTipo, avaliacao.Nota, avaliacao.Comentario, avaliacao.DataAvaliacao);
    }
}
