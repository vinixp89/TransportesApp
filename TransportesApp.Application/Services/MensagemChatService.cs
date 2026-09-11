using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Chat entre cliente e motorista durante uma corrida — quem decide QUANDO é permitido enviar
    // (status da corrida) e QUEM pode enviar/ler (participante da corrida) é o CorridasController,
    // que já tem os métodos de autorização prontos (UsuarioParticipaDaCorridaAsync); aqui só existe a
    // regra de persistência/mapeamento, sem repetir nenhuma dessas checagens.
    public class MensagemChatService
    {
        private readonly IMensagemChatRepository _mensagemChatRepository;

        public MensagemChatService(IMensagemChatRepository mensagemChatRepository)
        {
            _mensagemChatRepository = mensagemChatRepository;
        }

        public async Task<MensagemChatResponse> EnviarAsync(Guid corridaId, TipoUsuario remetenteTipo, string texto)
        {
            var mensagem = new MensagemChat(corridaId, remetenteTipo, texto);
            await _mensagemChatRepository.AdicionarAsync(mensagem);

            return MapearParaResponse(mensagem);
        }

        // "desde" alimenta o polling incremental do app (ver MensagensScreen) — sem ele, devolve o
        // histórico inteiro da corrida (usado só ao abrir o chat pela primeira vez).
        public async Task<IEnumerable<MensagemChatResponse>> ListarAsync(Guid corridaId, DateTime? desde)
        {
            var mensagens = await _mensagemChatRepository.ListarPorCorridaAsync(corridaId, desde);
            return mensagens.Select(MapearParaResponse);
        }

        private static MensagemChatResponse MapearParaResponse(MensagemChat mensagem)
            => new(mensagem.Id, mensagem.CorridaId, mensagem.RemetenteTipo, mensagem.Texto, mensagem.DataEnvio);
    }
}
