using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Registro de token (Expo push) por dispositivo e envio de notificações que chegam mesmo com o
    // app fechado (ver ExpoPushGateway). Hoje usado pelo Admin pra mandar avisos/promoções pros
    // Clientes; o mesmo broadcast por papel serve pro alerta de alta demanda pros Motoristas.
    public class PushNotificationService
    {
        private readonly IPushTokenRepository _pushTokenRepository;
        private readonly IExpoPushGateway _expoPushGateway;

        public PushNotificationService(IPushTokenRepository pushTokenRepository, IExpoPushGateway expoPushGateway)
        {
            _pushTokenRepository = pushTokenRepository;
            _expoPushGateway = expoPushGateway;
        }

        public async Task RegistrarTokenAsync(Guid usuarioId, TipoUsuario papel, string token)
        {
            var existente = await _pushTokenRepository.ObterPorTokenAsync(token);

            if (existente is null)
            {
                await _pushTokenRepository.AdicionarAsync(new PushToken(usuarioId, papel, token));
                return;
            }

            existente.AtualizarDono(usuarioId, papel);
            await _pushTokenRepository.AtualizarAsync(existente);
        }

        public async Task<int> EnviarBroadcastAsync(TipoUsuario papel, string titulo, string mensagem)
        {
            var tokens = await _pushTokenRepository.ListarTokensPorPapelAsync(papel);

            if (tokens.Count == 0)
                return 0;

            var tokensInvalidos = await _expoPushGateway.EnviarAsync(tokens, titulo, mensagem);

            foreach (var tokenInvalido in tokensInvalidos)
                await _pushTokenRepository.RemoverAsync(tokenInvalido);

            return tokens.Count - tokensInvalidos.Count;
        }
    }
}
