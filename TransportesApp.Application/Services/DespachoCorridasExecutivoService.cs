using Microsoft.Extensions.Logging;
using TransportesApp.Domain.Common;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Despacho das corridas Executivo, rodado a cada poucos segundos pelo
    // DespachoCorridasExecutivoBackgroundService:
    //  - assim que a corrida fica disponível, avisa por push (alta prioridade) os motoristas Executivo
    //    online, do mesmo tipo de veículo, a até RaioKm da origem — quem aceitar em até
    //    Corrida.JanelaAceiteRapido (1 min) fica com 90% do valor (ver Corrida.ObterPercentualMotorista);
    //  - passado 1 minuto sem aceite, reenvia o aviso pro mesmo grupo (aí o aceite paga 85%, o normal);
    //  - passados TempoLimite (5 min) sem aceite, cancela a corrida e devolve o que o cliente pagou.
    // Só corridas da categoria Executivo — as demais continuam só na lista do app, como antes.
    public class DespachoCorridasExecutivoService
    {
        public const double RaioKm = 5;
        public static readonly TimeSpan TempoLimite = TimeSpan.FromMinutes(5);

        private readonly ICorridaRepository _corridaRepository;
        private readonly IMotoristaRepository _motoristaRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly AssinaturaMotoristaExecutivoService _assinaturaExecutivoService;
        private readonly PushNotificationService _pushNotificationService;
        private readonly NotificacaoService _notificacaoService;
        private readonly CorridaService _corridaService;
        private readonly ILogger<DespachoCorridasExecutivoService> _logger;

        public DespachoCorridasExecutivoService(
            ICorridaRepository corridaRepository,
            IMotoristaRepository motoristaRepository,
            IClienteRepository clienteRepository,
            AssinaturaMotoristaExecutivoService assinaturaExecutivoService,
            PushNotificationService pushNotificationService,
            NotificacaoService notificacaoService,
            CorridaService corridaService,
            ILogger<DespachoCorridasExecutivoService> logger)
        {
            _corridaRepository = corridaRepository;
            _motoristaRepository = motoristaRepository;
            _clienteRepository = clienteRepository;
            _assinaturaExecutivoService = assinaturaExecutivoService;
            _pushNotificationService = pushNotificationService;
            _notificacaoService = notificacaoService;
            _corridaService = corridaService;
            _logger = logger;
        }

        public async Task ProcessarAsync()
        {
            var agora = DateTime.UtcNow;
            var corridas = await _corridaRepository.ListarSolicitadasPorCategoriaAsync(CategoriaCorrida.Executivo);

            foreach (var corrida in corridas)
            {
                // Corridas anteriores a esse recurso (sem DataDisponivel) ficam fora do despacho.
                if (corrida.DataDisponivel is null)
                    continue;

                try
                {
                    var decorrido = agora - corrida.DataDisponivel.Value;

                    if (decorrido >= TempoLimite)
                        await CancelarPorFaltaDeMotoristaAsync(corrida);
                    else if (corrida.AvisosMotoristasEnviados == 0)
                        await AvisarMotoristasAsync(corrida, reenvio: false);
                    else if (corrida.AvisosMotoristasEnviados == 1 && decorrido >= Corrida.JanelaAceiteRapido)
                        await AvisarMotoristasAsync(corrida, reenvio: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha no despacho da corrida Executivo {CorridaId}", corrida.Id);
                }
            }
        }

        private async Task AvisarMotoristasAsync(Corrida corrida, bool reenvio)
        {
            var disponiveis = await _motoristaRepository.ListarDisponiveisAsync();
            var destinatarios = new List<Guid>();

            foreach (var motorista in disponiveis)
            {
                if (motorista.TipoVeiculo != corrida.TipoVeiculo
                    || motorista.StatusConta != StatusContaMotorista.Ativa
                    || !motorista.FotosBasicasEnviadas
                    || motorista.LatitudeAtual is null || motorista.LongitudeAtual is null)
                    continue;

                var distancia = Geo.DistanciaKm(
                    motorista.LatitudeAtual.Value, motorista.LongitudeAtual.Value,
                    corrida.Origem.Latitude, corrida.Origem.Longitude);

                if (distancia > RaioKm)
                    continue;

                if (!await _assinaturaExecutivoService.EstaElegivelAsync(motorista.Id))
                    continue;

                destinatarios.Add(motorista.UsuarioId);
            }

            var titulo = reenvio ? "⏰ Corrida Executivo ainda disponível" : "🚨 Corrida Executivo perto de você";
            var mensagem = reenvio
                ? $"Faixa {corrida.FaixaContratada}, {corrida.DistanciaEstimadaKm:0.#} km. Ela ainda espera um motorista — aceite agora!"
                : $"Faixa {corrida.FaixaContratada}, {corrida.DistanciaEstimadaKm:0.#} km. Aceite em até 1 minuto e ganhe 90% do valor!";

            await _pushNotificationService.EnviarParaUsuariosAsync(
                destinatarios, TipoUsuario.Motorista, titulo, mensagem, altaPrioridade: true);

            // Registra o aviso mesmo sem destinatários — senão o serviço tentaria de novo a cada ciclo.
            corrida.RegistrarAvisoMotoristas();
            await _corridaRepository.AtualizarAsync(corrida);
        }

        // Cancelamento sempre com reembolso (não foi o cliente que desistiu): estorna o pagamento da
        // corrida avulsa ou devolve o pacote/benefício — ver CorridaService.CancelarAsync.
        private async Task CancelarPorFaltaDeMotoristaAsync(Corrida corrida)
        {
            await _corridaService.CancelarAsync(corrida.Id, sempreReembolsar: true);

            const string titulo = "Não encontramos motorista";
            const string mensagem =
                "Nenhum motorista Executivo aceitou sua corrida em 5 minutos, então ela foi cancelada e o que você pagou foi devolvido.";

            await _notificacaoService.CriarAsync(corrida.ClienteId, titulo, mensagem, TipoNotificacao.Geral);

            var cliente = await _clienteRepository.ObterPorIdAsync(corrida.ClienteId);
            if (cliente is not null)
            {
                await _pushNotificationService.EnviarParaUsuariosAsync(
                    new[] { cliente.UsuarioId }, TipoUsuario.Cliente, titulo, mensagem);
            }
        }
    }
}
