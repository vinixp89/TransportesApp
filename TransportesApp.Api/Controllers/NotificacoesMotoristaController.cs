using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    // Mesma caixa de entrada do NotificacoesController, só que pro lado do motorista — rota
    // separada (em vez de generalizar a mesma) porque o controller do cliente já é
    // [Authorize(Roles = "Cliente")] na classe inteira, e ASP.NET não deixa duas ações com a
    // mesma rota+verbo distinguidas só pela role.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Motorista")]
    public class NotificacoesMotoristaController : ControllerBase
    {
        private readonly NotificacaoService _notificacaoService;
        private readonly MotoristaService _motoristaService;

        public NotificacoesMotoristaController(NotificacaoService notificacaoService, MotoristaService motoristaService)
        {
            _notificacaoService = notificacaoService;
            _motoristaService = motoristaService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar notificações." });

            var notificacoes = await _notificacaoService.ListarPorMotoristaAsync(motorista.Id);
            return Ok(notificacoes);
        }

        [HttpGet("nao-lidas/contagem")]
        public async Task<IActionResult> ContarNaoLidas()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar notificações." });

            var contagem = await _notificacaoService.ContarNaoLidasPorMotoristaAsync(motorista.Id);
            return Ok(contagem);
        }

        [HttpPost("{id:guid}/marcar-lida")]
        public async Task<IActionResult> MarcarComoLida(Guid id)
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar notificações." });

            var marcou = await _notificacaoService.MarcarComoLidaPorMotoristaAsync(id, motorista.Id);

            if (!marcou)
                return NotFound();

            return NoContent();
        }

        [HttpPost("marcar-todas-lidas")]
        public async Task<IActionResult> MarcarTodasComoLidas()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar notificações." });

            await _notificacaoService.MarcarTodasComoLidasPorMotoristaAsync(motorista.Id);
            return NoContent();
        }

        private async Task<Application.DTOs.MotoristaResponse?> ObterMotoristaLogadoAsync()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            return await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);
        }
    }
}
