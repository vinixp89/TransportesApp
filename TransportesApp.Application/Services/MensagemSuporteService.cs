using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Chat de suporte entre Cliente/Motorista e o Admin — quem decide QUEM pode enviar/ler (dono da
    // conversa ou Admin) é o controller; aqui só existe a regra de persistência/mapeamento.
    public class MensagemSuporteService
    {
        private readonly IMensagemSuporteRepository _mensagemSuporteRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IMotoristaRepository _motoristaRepository;

        public MensagemSuporteService(
            IMensagemSuporteRepository mensagemSuporteRepository,
            IClienteRepository clienteRepository,
            IMotoristaRepository motoristaRepository)
        {
            _mensagemSuporteRepository = mensagemSuporteRepository;
            _clienteRepository = clienteRepository;
            _motoristaRepository = motoristaRepository;
        }

        public async Task<MensagemSuporteResponse> EnviarAsync(Guid usuarioId, TipoUsuario tipoUsuario, string texto)
        {
            var mensagem = new MensagemSuporte(usuarioId, tipoUsuario, enviadaPeloAdmin: false, texto);
            await _mensagemSuporteRepository.AdicionarAsync(mensagem);

            return MapearParaResponse(mensagem);
        }

        // Usado tanto pelo próprio Cliente/Motorista (sua conversa) quanto pelo Admin (conversa de
        // um usuário específico) — quem pode chamar com qual usuarioId é responsabilidade do
        // controller.
        public async Task<IEnumerable<MensagemSuporteResponse>> ListarPorUsuarioAsync(Guid usuarioId, DateTime? desde)
        {
            var mensagens = await _mensagemSuporteRepository.ListarPorUsuarioAsync(usuarioId, desde);
            return mensagens.Select(MapearParaResponse);
        }

        // Responder como Admin exige saber o TipoUsuario da conversa (pra gravar certo e pro app
        // filtrar depois) — como isso não é cadastrado em nenhum outro lugar, pega da última
        // mensagem já existente dessa conversa. Sem mensagem anterior, não existe conversa pra
        // responder.
        public async Task<MensagemSuporteResponse?> ResponderComoAdminAsync(Guid usuarioId, string texto)
        {
            var ultima = await _mensagemSuporteRepository.ObterUltimaPorUsuarioAsync(usuarioId);

            if (ultima is null)
                return null;

            var mensagem = new MensagemSuporte(usuarioId, ultima.TipoUsuario, enviadaPeloAdmin: true, texto);
            await _mensagemSuporteRepository.AdicionarAsync(mensagem);

            return MapearParaResponse(mensagem);
        }

        // Conversas cuja última mensagem é do usuário (Admin ainda não respondeu) — usado pro aviso de
        // "nova mensagem" no painel, que consulta isso a cada poucos segundos: por isso não monta a
        // lista completa nem busca o nome de cada usuário, como ListarConversasAsync faz.
        public async Task<int> ContarConversasPendentesAsync()
        {
            var ultimas = await _mensagemSuporteRepository.ListarUltimaMensagemDeCadaConversaAsync();
            return ultimas.Count(m => !m.EnviadaPeloAdmin);
        }

        public async Task<IEnumerable<ConversaSuporteResponse>> ListarConversasAsync()
        {
            var ultimas = await _mensagemSuporteRepository.ListarUltimaMensagemDeCadaConversaAsync();

            var resultado = new List<ConversaSuporteResponse>();

            foreach (var ultima in ultimas)
            {
                var nome = ultima.TipoUsuario == TipoUsuario.Cliente
                    ? (await _clienteRepository.ObterPorUsuarioIdAsync(ultima.UsuarioId))?.Nome
                    : (await _motoristaRepository.ObterPorUsuarioIdAsync(ultima.UsuarioId))?.Nome;

                resultado.Add(new ConversaSuporteResponse(
                    ultima.UsuarioId,
                    ultima.TipoUsuario,
                    nome ?? "—",
                    ultima.Texto,
                    ultima.DataEnvio,
                    PendenteResposta: !ultima.EnviadaPeloAdmin));
            }

            return resultado;
        }

        private static MensagemSuporteResponse MapearParaResponse(MensagemSuporte mensagem)
            => new(mensagem.Id, mensagem.UsuarioId, mensagem.TipoUsuario, mensagem.EnviadaPeloAdmin, mensagem.Texto, mensagem.DataEnvio);
    }
}
