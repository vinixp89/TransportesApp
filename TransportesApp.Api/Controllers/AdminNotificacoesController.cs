using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/notificacoes")]
    [Authorize(Roles = "Admin")]
    public class AdminNotificacoesController : ControllerBase
    {
        private readonly PushNotificationService _pushNotificationService;

        public AdminNotificacoesController(PushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;
        }

        [HttpPost("broadcast")]
        public async Task<ActionResult<BroadcastEnviadoResponse>> Broadcast(EnviarBroadcastRequest request)
        {
            var totalEnviado = await _pushNotificationService.EnviarBroadcastAsync(request.Papel, request.Titulo, request.Mensagem);
            return Ok(new BroadcastEnviadoResponse(totalEnviado));
        }
    }
}
