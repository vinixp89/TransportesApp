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

            try
            {
                var corrida = await _corridaService.CriarAsync(request, cliente.Id);
                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
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

            if (!await UsuarioParticipaDaCorridaAsync(corrida))
                return Forbid();

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
            var corridaAtual = await _corridaService.ObterPorIdAsync(id);

            if (corridaAtual is null)
                return NotFound();

            if (!await MotoristaAtribuidoNaCorridaAsync(corridaAtual))
                return Forbid();

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
            var corridaAtual = await _corridaService.ObterPorIdAsync(id);

            if (corridaAtual is null)
                return NotFound();

            if (!await MotoristaAtribuidoNaCorridaAsync(corridaAtual))
                return Forbid();

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
            var corridaAtual = await _corridaService.ObterPorIdAsync(id);

            if (corridaAtual is null)
                return NotFound();

            if (!await UsuarioParticipaDaCorridaAsync(corridaAtual))
                return Forbid();

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

        // Admin sempre pode; caso contrário, só o cliente dono da corrida ou o motorista atribuído a ela.
        private async Task<bool> UsuarioParticipaDaCorridaAsync(CorridaResponse corrida)
        {
            if (User.IsInRole("Admin"))
                return true;

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is not null && cliente.Id == corrida.ClienteId)
                return true;

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is not null && corrida.MotoristaId is not null && motorista.Id == corrida.MotoristaId)
                return true;

            return false;
        }

        // Só o motorista atribuído àquela corrida especificamente (não qualquer motorista).
        private async Task<bool> MotoristaAtribuidoNaCorridaAsync(CorridaResponse corrida)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            return motorista is not null && corrida.MotoristaId is not null && motorista.Id == corrida.MotoristaId;
        }
    }
}