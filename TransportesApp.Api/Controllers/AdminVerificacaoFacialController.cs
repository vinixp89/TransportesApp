using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    // Painel de revisão da auditoria de reconhecimento facial (ver VerificacaoFacial) — lista as
    // comparações feitas ao iniciar/finalizar corridas, com a foto capturada e a selfie de cadastro
    // lado a lado pro Admin conferir manualmente. Mesmo padrão de streaming autenticado de
    // AdminExecutivoController.
    [ApiController]
    [Route("api/admin/verificacao-facial")]
    [Authorize(Roles = "Admin")]
    public class AdminVerificacaoFacialController : ControllerBase
    {
        private readonly VerificacaoFacialService _verificacaoFacialService;
        private readonly MotoristaService _motoristaService;
        private readonly IWebHostEnvironment _ambiente;

        public AdminVerificacaoFacialController(
            VerificacaoFacialService verificacaoFacialService,
            MotoristaService motoristaService,
            IWebHostEnvironment ambiente)
        {
            _verificacaoFacialService = verificacaoFacialService;
            _motoristaService = motoristaService;
            _ambiente = ambiente;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int quantidade = 100)
        {
            var verificacoes = await _verificacaoFacialService.ListarRecentesAsync(quantidade);
            return Ok(verificacoes);
        }

        // Foto capturada na hora (início ou fim da corrida).
        [HttpGet("{id:guid}/foto")]
        public async Task<IActionResult> ObterFotoCapturada(Guid id)
            => await ServirFotoAsync(await _verificacaoFacialService.ObterCaminhoFotoAsync(id));

        // Selfie original de cadastro do motorista, pra comparar lado a lado com a foto capturada.
        [HttpGet("motoristas/{motoristaId:guid}/foto-referencia")]
        public async Task<IActionResult> ObterFotoReferencia(Guid motoristaId)
            => await ServirFotoAsync(await _motoristaService.ObterCaminhoFotoSelfieAsync(motoristaId));

        private Task<IActionResult> ServirFotoAsync(string? caminhoRelativo)
        {
            if (caminhoRelativo is null)
                return Task.FromResult<IActionResult>(NotFound());

            var caminhoCompleto = Path.Combine(_ambiente.ContentRootPath, "uploads", caminhoRelativo);

            if (!System.IO.File.Exists(caminhoCompleto))
                return Task.FromResult<IActionResult>(NotFound());

            var contentType = Path.GetExtension(caminhoCompleto).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                _ => "image/jpeg"
            };

            return Task.FromResult<IActionResult>(PhysicalFile(caminhoCompleto, contentType));
        }
    }
}
