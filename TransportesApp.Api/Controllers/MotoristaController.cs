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
    public class MotoristasController : ControllerBase
    {
        private readonly MotoristaService _motoristaService;

        public MotoristasController(MotoristaService motoristaService)
        {
            _motoristaService = motoristaService;
        }

        [Authorize(Roles = "Motorista")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarMotoristaRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.CriarAsync(request, usuarioId);
            return Ok(motorista);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var motoristas = await _motoristaService.ListarAsync();
            return Ok(motoristas);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("disponiveis")]
        public async Task<IActionResult> ListarDisponiveis()
        {
            var motoristas = await _motoristaService.ListarDisponiveisAsync();
            return Ok(motoristas);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var motorista = await _motoristaService.ObterPorIdAsync(id);

            if (motorista is null)
                return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")!);

                if (motorista.UsuarioId != usuarioId)
                    return Forbid();
            }

            return Ok(motorista);
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("ficar-disponivel")]
        public async Task<IActionResult> FicarDisponivel()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.FicarDisponivelAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de ficar disponível." });

            return Ok(motorista);
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("ficar-offline")]
        public async Task<IActionResult> FicarOffline()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.FicarOfflineAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de alterar o status." });

            return Ok(motorista);
        }
    }
}