using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var motorista = await _motoristaService.CriarAsync(request);
            return Ok(motorista);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var motorista = await _motoristaService.ObterPorIdAsync(id);

            if (motorista is null)
                return NotFound();

            return Ok(motorista);
        }
    }
}