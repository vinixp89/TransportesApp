using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromocoesController : ControllerBase
    {
        private readonly PromocaoLancamentoService _promocaoLancamentoService;

        public PromocoesController(PromocaoLancamentoService promocaoLancamentoService)
        {
            _promocaoLancamentoService = promocaoLancamentoService;
        }

        // Público de propósito — dá pra mostrar "restam X vagas!" na tela de cadastro sem precisar
        // estar logado, e sem expor nada além de números.
        [AllowAnonymous]
        [HttpGet("lancamento")]
        public async Task<IActionResult> Lancamento()
        {
            var status = await _promocaoLancamentoService.ObterStatusAsync();
            return Ok(status);
        }
    }
}
