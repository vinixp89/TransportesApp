using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    // Não existe integração com gateway pra saque (só pra cobrança) — quem transfere o dinheiro pro
    // motorista de verdade é o Admin, manualmente (Pix/TED), fora do app. Esses endpoints só servem
    // pra ele acompanhar o que está pendente e registrar aqui depois de pagar (ou rejeitar, com
    // motivo). Sem tela própria no painel ainda — usar via Swagger/Postman logado como Admin.
    [ApiController]
    [Route("api/admin/saques")]
    [Authorize(Roles = "Admin")]
    public class AdminSaquesController : ControllerBase
    {
        private readonly CarteiraMotoristaService _carteiraMotoristaService;

        public AdminSaquesController(CarteiraMotoristaService carteiraMotoristaService)
        {
            _carteiraMotoristaService = carteiraMotoristaService;
        }

        [HttpGet("pendentes")]
        public async Task<IActionResult> Pendentes()
        {
            var pendentes = await _carteiraMotoristaService.ListarPendentesAsync();
            return Ok(pendentes);
        }

        [HttpPost("{id:guid}/concluir")]
        public async Task<IActionResult> Concluir(Guid id)
        {
            try
            {
                var resultado = await _carteiraMotoristaService.ConcluirSaqueAsync(id);

                if (resultado is null)
                    return NotFound(new { mensagem = "Solicitação de saque não encontrada." });

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/rejeitar")]
        public async Task<IActionResult> Rejeitar(Guid id, [FromBody] RejeitarSaqueRequest request)
        {
            try
            {
                var resultado = await _carteiraMotoristaService.RejeitarSaqueAsync(id, request.Motivo);

                if (resultado is null)
                    return NotFound(new { mensagem = "Solicitação de saque não encontrada." });

                return Ok(resultado);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
