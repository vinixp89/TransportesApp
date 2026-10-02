using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    // Lado do Admin do chat de suporte (ver SuporteController pro lado do Cliente/Motorista) — lista
    // as conversas em aberto e permite responder qualquer uma.
    [ApiController]
    [Route("api/admin/suporte")]
    [Authorize(Roles = "Admin")]
    public class AdminSuporteController : ControllerBase
    {
        private readonly MensagemSuporteService _mensagemSuporteService;

        public AdminSuporteController(MensagemSuporteService mensagemSuporteService)
        {
            _mensagemSuporteService = mensagemSuporteService;
        }

        [HttpGet("conversas")]
        public async Task<IActionResult> ListarConversas()
        {
            var conversas = await _mensagemSuporteService.ListarConversasAsync();
            return Ok(conversas);
        }

        [HttpGet("conversas/{usuarioId:guid}/mensagens")]
        public async Task<IActionResult> ListarMensagens(Guid usuarioId, [FromQuery] DateTime? desde)
        {
            var mensagens = await _mensagemSuporteService.ListarPorUsuarioAsync(usuarioId, desde);
            return Ok(mensagens);
        }

        [HttpPost("conversas/{usuarioId:guid}/responder")]
        public async Task<IActionResult> Responder(Guid usuarioId, [FromBody] EnviarMensagemSuporteRequest request)
        {
            try
            {
                var mensagem = await _mensagemSuporteService.ResponderComoAdminAsync(usuarioId, request.Texto);

                if (mensagem is null)
                    return NotFound(new { mensagem = "Essa conversa não existe." });

                return Ok(mensagem);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
