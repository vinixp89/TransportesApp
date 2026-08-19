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
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _clienteService;

        public ClientesController(ClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarClienteRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")!;

            var cliente = await _clienteService.CriarAsync(request, usuarioId, email);
            return Ok(cliente);
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var clientes = await _clienteService.ListarAsync();
            return Ok(clientes);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var cliente = await _clienteService.ObterPorIdAsync(id);

            if (cliente is null)
                return NotFound();

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            if (cliente.UsuarioId != usuarioId)
                return Forbid();

            return Ok(cliente);
        }
    }
}