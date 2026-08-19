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
    public class CorridasController : ControllerBase
    {
        private readonly CorridaService _corridaService;
        private readonly ClienteService _clienteService;
        private readonly MotoristaService _motoristaService;

        public CorridasController(CorridaService corridaService, ClienteService clienteService, MotoristaService motoristaService)
        {
            _corridaService = corridaService;
            _clienteService = clienteService;
            _motoristaService = motoristaService;
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarCorridasRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de solicitar corridas." });

            var corrida = await _corridaService.CriarAsync(request, cliente.Id);
            return Ok(corrida);
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var corridas = await _corridaService.ListarAsync();
            return Ok(corridas);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            return Ok(corrida);
        }


    

    [Authorize(Roles = "Motorista")]
    [HttpPatch("{id}/atribuir-motorista")]
        public async Task<IActionResult> AtribuirMotorista(Guid id)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de aceitar corridas." });

            try
            {
                var corrida = await _corridaService.AtribuirMotoristaAsync(id, motorista.Id);

                if (corrida is null)
                    return NotFound();

                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Motorista")]
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

        [Authorize(Roles = "Motorista")]
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