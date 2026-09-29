using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;
using TransportesApp.Domain.Enums;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PushTokensController : ControllerBase
    {
        private readonly PushNotificationService _pushNotificationService;

        public PushTokensController(PushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;
        }

        // Chamado pelo app (Cliente ou Motorista) assim que o usuário loga e concede a permissão de
        // notificação — o papel vem da role do próprio token JWT, não do corpo da requisição.
        [HttpPost]
        public async Task<IActionResult> Registrar(RegistrarPushTokenRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);
            var papel = User.IsInRole("Cliente") ? TipoUsuario.Cliente : TipoUsuario.Motorista;

            await _pushNotificationService.RegistrarTokenAsync(usuarioId, papel, request.Token);
            return NoContent();
        }
    }
}
