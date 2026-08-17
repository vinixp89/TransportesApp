using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorridasController : ControllerBase
    {
        private readonly CorridaService _corridaService;

        public CorridasController(CorridaService corridaService)
        {
            _corridaService = corridaService;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarCorridasRequest request)
        {
            var corrida = await _corridaService.CriarAsync(request);
            return Ok(corrida);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            return Ok(corrida);
        }
    }
}