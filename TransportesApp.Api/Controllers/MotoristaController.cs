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

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarMotoristaRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.CriarAsync(request, usuarioId);
            return Ok(motorista);
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var motoristas = await _motoristaService.ListarAsync();
            return Ok(motoristas);
        }

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

            return Ok(motorista);
        }
    }
}