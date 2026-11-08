using TransportesApp.Application.Services;

namespace TransportesApp.Api.BackgroundServices
{
    // Roda o DespachoCorridasExecutivoService a cada poucos segundos (aviso pros motoristas perto,
    // reenvio depois de 1 min e cancelamento depois de 5 min sem aceite). Intervalo curto porque a
    // janela de aceite rápido é de 60s: um ciclo de 10s mantém o reenvio e o cancelamento a no
    // máximo ~10s do prazo.
    public class DespachoCorridasExecutivoBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DespachoCorridasExecutivoBackgroundService> _logger;

        public DespachoCorridasExecutivoBackgroundService(
            IServiceScopeFactory scopeFactory, ILogger<DespachoCorridasExecutivoBackgroundService> logger)
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
                    using var scope = _scopeFactory.CreateScope();
                    var despacho = scope.ServiceProvider.GetRequiredService<DespachoCorridasExecutivoService>();
                    await despacho.ProcessarAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao rodar o despacho das corridas Executivo.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
