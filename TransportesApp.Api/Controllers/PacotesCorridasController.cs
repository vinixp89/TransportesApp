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
    public class PacotesCorridasController : ControllerBase
    {
        private readonly PacoteCorridasService _pacoteCorridasService;
        private readonly ClienteService _clienteService;

        public PacotesCorridasController(PacoteCorridasService pacoteCorridasService, ClienteService clienteService)
        {
            _pacoteCorridasService = pacoteCorridasService;
            _clienteService = clienteService;
        }

        // Lista as faixas com o preço avulso e o preço calculado de cada tamanho de pacote —
        // usado pra montar a tela "pacotes disponíveis" antes de comprar. Não depende de cliente
        // cadastrado, só de estar autenticado (herda o [Authorize] da classe).
        [HttpGet("catalogo")]
        public IActionResult Catalogo()
        {
            var catalogo = _pacoteCorridasService.ObterCatalogo();
            return Ok(catalogo);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarPacoteCorridasRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de comprar um pacote." });

            try
            {
                var pacote = await _pacoteCorridasService.CriarAsync(request, cliente.Id);
                return Ok(pacote);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet("meus-pacotes")]
        public async Task<IActionResult> MeusPacotes()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar pacotes." });

            var pacotes = await _pacoteCorridasService.ListarPorClienteAsync(cliente.Id);
            return Ok(pacotes);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var pacote = await _pacoteCorridasService.ObterPorIdAsync(id);

            if (pacote is null)
                return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")!);

                var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

                if (cliente is null || pacote.ClienteId != cliente.Id)
                    return Forbid();
            }

            return Ok(pacote);
        }
    }
}
