using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanosController : ControllerBase
    {
        private readonly PlanoService _planoService;
        private readonly ClienteService _clienteService;

        public PlanosController(PlanoService planoService, ClienteService clienteService)
        {
            _planoService = planoService;
            _clienteService = clienteService;
        }

        // Catálogo é público pra qualquer usuário autenticado ver (não só Cliente) — só assinar
        // de fato é que exige ser Cliente.
        [HttpGet("catalogo")]
        public IActionResult Catalogo()
        {
            return Ok(_planoService.ObterCatalogo());
        }

        [HttpGet("minha-assinatura")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> MinhaAssinatura()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar sua assinatura." });

            var assinatura = await _planoService.ObterAssinaturaAtualAsync(cliente.Id);
            return Ok(assinatura);
        }

        [HttpGet("beneficio")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Beneficio()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar seu benefício." });

            var beneficio = await _planoService.ObterBeneficioAsync(cliente.Id);
            return Ok(beneficio);
        }

        [HttpPost("assinar")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Assinar([FromBody] AssinarPlanoRequest request)
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de assinar um plano." });

            var assinatura = await _planoService.AssinarAsync(cliente.Id, request.Tipo);
            return Ok(assinatura);
        }

        [HttpPost("cancelar")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Cancelar()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de cancelar uma assinatura." });

            var cancelou = await _planoService.CancelarAsync(cliente.Id);

            if (!cancelou)
                return BadRequest(new { mensagem = "Você não tem uma assinatura ativa pra cancelar." });

            return NoContent();
        }

        private async Task<ClienteResponse?> ObterClienteLogadoAsync()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            return await _clienteService.ObterPorUsuarioIdAsync(usuarioId);
        }
    }
}
