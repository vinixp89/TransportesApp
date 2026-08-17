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


    

    [HttpPatch("{id}/atribuir-motorista")]
        public async Task<IActionResult> AtribuirMotorista(Guid id, [FromBody] AtribuirMotoristaRequest request)
        {
            try
            {
                var corrida = await _corridaService.AtribuirMotoristaAsync(id, request);

                if (corrida is null)
                    return NotFound();

                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPatch("{id}/iniciar")]
        public async Task<IActionResult> IniciarViagem(Guid id)
        {
            try
            {
                var corrida = await _corridaService.IniciarViagemAsync(id);

                if (corrida is null)
                    return NotFound();

                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPatch("{id}/finalizar")]
        public async Task<IActionResult> Finalizar(Guid id, [FromBody] FinalizarCorridaRequest request)
        {
            try
            {
                var resultado = await _corridaService.FinalizarAsync(id, request);

                if (resultado is null)
                    return NotFound();

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid id)
        {
            try
            {
                var corrida = await _corridaService.CancelarAsync(id);

                if (corrida is null)
                    return NotFound();

                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    } }