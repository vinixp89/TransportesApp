using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    // Aviso configurável pelo Admin, mostrado como pop-up ao abrir o app Cliente (ver AvisoService).
    [ApiController]
    [Route("api/[controller]")]
    public class AvisosController : ControllerBase
    {
        private readonly AvisoService _avisoService;

        public AvisosController(AvisoService avisoService)
        {
            _avisoService = avisoService;
        }

        // Público de propósito — o app busca isso antes de logar (e depois de logar também), pra
        // decidir se mostra o pop-up na Home. Retorna null (200 com corpo vazio) quando não tem
        // nenhum aviso vigente.
        [AllowAnonymous]
        [HttpGet("ativo")]
        public async Task<IActionResult> Ativo()
        {
            var aviso = await _avisoService.ObterAtivoAsync();
            return Ok(aviso);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var avisos = await _avisoService.ListarAsync();
            return Ok(avisos);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarAvisoRequest request)
        {
            try
            {
                var aviso = await _avisoService.CriarAsync(request);
                return Ok(aviso);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarAvisoRequest request)
        {
            try
            {
                var aviso = await _avisoService.AtualizarAsync(id, request);
                return Ok(aviso);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id)
        {
            try
            {
                await _avisoService.ExcluirAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
