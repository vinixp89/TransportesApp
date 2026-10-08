using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    // Só o Admin vê quantas vagas do bônus de boas-vindas ainda restam (ver BonusMotoristaService) —
    // o app Motorista não tem nenhum endpoint com esse número.
    [ApiController]
    [Route("api/admin/bonus-motorista")]
    [Authorize(Roles = "Admin")]
    public class AdminBonusMotoristaController : ControllerBase
    {
        private readonly BonusMotoristaService _bonusMotoristaService;

        public AdminBonusMotoristaController(BonusMotoristaService bonusMotoristaService)
        {
            _bonusMotoristaService = bonusMotoristaService;
        }

        [HttpGet("lista")]
        public async Task<IActionResult> Listar()
        {
            return Ok(await _bonusMotoristaService.ListarAsync());
        }

        [HttpGet("status")]
        public async Task<IActionResult> ObterStatus()
        {
            return Ok(await _bonusMotoristaService.ObterStatusAsync());
        }
    }
}
