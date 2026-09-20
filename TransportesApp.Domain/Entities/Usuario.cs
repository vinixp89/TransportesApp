using Microsoft.AspNetCore.Identity;

namespace TransportesApp.Domain.Entities
{
    public class Usuario : IdentityUser<Guid>
    {
        // Sessão única: todo login novo gera um Guid novo aqui e embute ele no JWT (claim "sessao").
        // A cada requisição autenticada, o middleware (ver Program.cs, JwtBearerEvents.
        // OnTokenValidated) confere se o token apresentado ainda tem o valor mais recente — se não
        // tiver, foi feito um login mais novo em outro aparelho, então esse token é recusado (401).
        public Guid? SessaoAtualId { get; set; }

        // Última requisição autenticada bem-sucedida — usado só pro painel do Admin estimar quantos
        // usuários estão "logados agora" (ver AdminUsuariosController). Atualizado no mesmo evento
        // acima, com throttle (só grava se fizer mais de 1 minuto da última atualização) pra não virar
        // um UPDATE a cada requisição.
        public DateTime? UltimoAcessoEm { get; set; }
    }
}