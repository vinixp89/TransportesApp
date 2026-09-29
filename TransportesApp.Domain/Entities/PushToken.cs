using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Entities
{
    // Token do Expo Push Notifications (ExponentPushToken[...]) de um dispositivo, usado pra mandar
    // notificação que chega mesmo com o app fechado (ver ExpoPushGateway). Um mesmo token físico pode
    // trocar de dono se a pessoa reinstalar o app ou logar com outra conta no mesmo aparelho — por
    // isso é único por token, e reaparecer só atualiza o dono em vez de duplicar (ver
    // PushNotificationService.RegistrarTokenAsync).
    public class PushToken
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public TipoUsuario Papel { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime DataAtualizacao { get; private set; }

        protected PushToken() { }

        public PushToken(Guid usuarioId, TipoUsuario papel, string token)
        {
            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            Papel = papel;
            Token = token;
            DataAtualizacao = DateTime.UtcNow;
        }

        public void AtualizarDono(Guid usuarioId, TipoUsuario papel)
        {
            UsuarioId = usuarioId;
            Papel = papel;
            DataAtualizacao = DateTime.UtcNow;
        }
    }
}
