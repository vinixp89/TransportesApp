using Microsoft.AspNetCore.Mvc;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MotoristasController : ControllerBase
    {
        private readonly IMotoristaRepository _motoristaRepository;

        public MotoristasController(IMotoristaRepository motoristaRepository)
        {
            _motoristaRepository = motoristaRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Criar()
        {
            var motorista = new Motorista(
                usuarioId: Guid.NewGuid(),
                cnh: "12345678900",
                placaVeiculo: "ABC1D23",
                modeloVeiculo: "Fiat Uno"
            );

            await _motoristaRepository.AdicionarAsync(motorista);

            return Ok(motorista);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var motorista = await _motoristaRepository.ObterPorIdAsync(id);

            if (motorista is null)
                return NotFound();

            return Ok(motorista);
        }
    }
}