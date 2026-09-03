using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class NotificacoesController : ControllerBase
    {
        private readonly NotificacaoService _notificacaoService;
        private readonly ClienteService _clienteService;

        public NotificacoesController(NotificacaoService notificacaoService, ClienteService clienteService)
        {
            _notificacaoService = notificacaoService;
            _clienteService = clienteService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar notificações." });

            var notificacoes = await _notificacaoService.ListarAsync(cliente.Id);
            return Ok(notificacoes);
        }

        [HttpGet("nao-lidas/contagem")]
        public async Task<IActionResult> ContarNaoLidas()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar notificações." });

            var contagem = await _notificacaoService.ContarNaoLidasAsync(cliente.Id);
            return Ok(contagem);
        }

        [HttpPost("{id:guid}/marcar-lida")]
        public async Task<IActionResult> MarcarComoLida(Guid id)
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar notificações." });

            var marcou = await _notificacaoService.MarcarComoLidaAsync(id, cliente.Id);

            if (!marcou)
                return NotFound();

            return NoContent();
        }

        [HttpPost("marcar-todas-lidas")]
        public async Task<IActionResult> MarcarTodasComoLidas()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar notificações." });

            await _notificacaoService.MarcarTodasComoLidasAsync(cliente.Id);
            return NoContent();
        }

        private async Task<Application.DTOs.ClienteResponse?> ObterClienteLogadoAsync()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            return await _clienteService.ObterPorUsuarioIdAsync(usuarioId);
        }
    }
}
