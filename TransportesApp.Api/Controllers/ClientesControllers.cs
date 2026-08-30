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
        private readonly DoacaoService _doacaoService;

        public ClientesController(ClienteService clienteService, DoacaoService doacaoService)
        {
            _clienteService = clienteService;
            _doacaoService = doacaoService;
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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var clientes = await _clienteService.ListarAsync();
            return Ok(clientes);
        }

        // Busca de destinatário pra doar uma corrida (ver CarteirasController.Doar) — só por e-mail
        // exato, nunca por nome, pra não virar uma lista pesquisável de todos os clientes.
        [Authorize(Roles = "Cliente")]
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { mensagem = "Informe o e-mail do destinatário." });

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var clienteLogado = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (clienteLogado is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de buscar outro cliente." });

            var resultado = await _doacaoService.BuscarPorEmailAsync(email, clienteLogado.Id);

            if (resultado is null)
                return NotFound(new { mensagem = "Nenhum cliente encontrado com esse e-mail." });

            return Ok(resultado);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var cliente = await _clienteService.ObterPorIdAsync(id);

            if (cliente is null)
                return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")!);

                if (cliente.UsuarioId != usuarioId)
                    return Forbid();
            }

            return Ok(cliente);
        }
    }
}