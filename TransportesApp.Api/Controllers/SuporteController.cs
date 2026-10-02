using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;
using TransportesApp.Domain.Enums;

namespace TransportesApp.Api.Controllers
{
    // Chat de suporte do Cliente/Motorista com o Admin, fora do contexto de uma corrida (ver
    // AdminSuporteController pro lado de quem responde). Mesmo padrão de polling do chat de
    // corrida: GET com "desde" pra incremental, POST pra enviar.
    [ApiController]
    [Route("api/suporte")]
    [Authorize(Roles = "Cliente,Motorista")]
    public class SuporteController : ControllerBase
    {
        private readonly MensagemSuporteService _mensagemSuporteService;

        public SuporteController(MensagemSuporteService mensagemSuporteService)
        {
            _mensagemSuporteService = mensagemSuporteService;
        }

        [HttpPost("mensagens")]
        public async Task<IActionResult> Enviar([FromBody] EnviarMensagemSuporteRequest request)
        {
            var usuarioId = ObterUsuarioId();
            var tipoUsuario = User.IsInRole("Cliente") ? TipoUsuario.Cliente : TipoUsuario.Motorista;

            try
            {
                var mensagem = await _mensagemSuporteService.EnviarAsync(usuarioId, tipoUsuario, request.Texto);
                return Ok(mensagem);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("mensagens")]
        public async Task<IActionResult> Listar([FromQuery] DateTime? desde)
        {
            var usuarioId = ObterUsuarioId();
            var mensagens = await _mensagemSuporteService.ListarPorUsuarioAsync(usuarioId, desde);
            return Ok(mensagens);
        }

        private Guid ObterUsuarioId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
    }
}
