using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Api.BackgroundServices
{
    // Rede de segurança contra cadastros "presos": o AuthController já limpa o usuário Identity na
    // hora se a criação do perfil (Motorista/Cliente) falhar (ver RegistrarAsync), mas isso não cobre
    // o processo cair no meio da requisição ou qualquer outro cenário fora do try/catch. Sem essa
    // limpeza, o e-mail/telefone dessa conta ficaria bloqueado pra sempre numa tentativa futura de
    // cadastro, mesmo sem nenhum perfil de verdade associado.
    //
    // Só mexe em contas Cliente/Motorista (Admin nunca passa por esse fluxo de cadastro) e só apaga
    // depois de TempoDeGracaMinutos pra não correr o risco de apagar um cadastro genuinamente em
    // andamento no meio de uma requisição concorrente.
    public class LimpezaCadastrosOrfaosService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);
        private const int TempoDeGracaMinutos = 15;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LimpezaCadastrosOrfaosService> _logger;

        public LimpezaCadastrosOrfaosService(IServiceScopeFactory scopeFactory, ILogger<LimpezaCadastrosOrfaosService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Intervalo);

            do
            {
                try
                {
                    await LimparOrfaosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao rodar a limpeza de cadastros órfãos.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task LimparOrfaosAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var limite = DateTime.UtcNow.AddMinutes(-TempoDeGracaMinutos);

            await RemoverOrfaosDaRoleAsync(
                userManager,
                "Motorista",
                limite,
                usuarioId => context.Motoristas.AnyAsync(m => m.UsuarioId == usuarioId, ct),
                ct);

            await RemoverOrfaosDaRoleAsync(
                userManager,
                "Cliente",
                limite,
                usuarioId => context.Clientes.AnyAsync(c => c.UsuarioId == usuarioId, ct),
                ct);
        }

        private async Task RemoverOrfaosDaRoleAsync(
            UserManager<Usuario> userManager,
            string role,
            DateTime limite,
            Func<Guid, Task<bool>> temPerfilAsync,
            CancellationToken ct)
        {
            var candidatos = (await userManager.GetUsersInRoleAsync(role))
                .Where(u => u.DataCriacao < limite);

            foreach (var usuario in candidatos)
            {
                ct.ThrowIfCancellationRequested();

                if (await temPerfilAsync(usuario.Id))
                    continue;

                await userManager.DeleteAsync(usuario);
                _logger.LogInformation(
                    "Cadastro órfão de {Role} removido pela limpeza automática: {UsuarioId} ({Email})",
                    role, usuario.Id, usuario.Email);
            }
        }
    }
}
