using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    // Revisão manual da categoria Executivo antes de qualquer cobrança — ver
    // AssinaturaMotoristaExecutivoService.AssinarAsync (placa/modelo/ano são autodeclarados, então o
    // Admin confere a foto do carro/placa aqui antes de aprovar). Mesmo padrão do
    // AdminSaquesController (pendentes → aprovar/negar).
    [ApiController]
    [Route("api/admin/executivo")]
    [Authorize(Roles = "Admin")]
    public class AdminExecutivoController : ControllerBase
    {
        private readonly AssinaturaMotoristaExecutivoService _assinaturaExecutivoService;
        private readonly MotoristaService _motoristaService;
        private readonly IWebHostEnvironment _ambiente;

        public AdminExecutivoController(
            AssinaturaMotoristaExecutivoService assinaturaExecutivoService,
            MotoristaService motoristaService,
            IWebHostEnvironment ambiente)
        {
            _assinaturaExecutivoService = assinaturaExecutivoService;
            _motoristaService = motoristaService;
            _ambiente = ambiente;
        }

        [HttpGet("pendentes")]
        public async Task<IActionResult> Pendentes()
        {
            var pendentes = await _assinaturaExecutivoService.ListarAguardandoAprovacaoAsync();
            return Ok(pendentes);
        }

        [HttpPost("{id:guid}/aprovar")]
        public async Task<IActionResult> Aprovar(Guid id)
        {
            try
            {
                var resultado = await _assinaturaExecutivoService.AprovarAsync(id);

                if (resultado is null)
                    return NotFound(new { mensagem = "Solicitação não encontrada." });

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id:guid}/negar")]
        public async Task<IActionResult> Negar(Guid id, [FromBody] NegarExecutivoRequest request)
        {
            try
            {
                var resultado = await _assinaturaExecutivoService.NegarAsync(id, request.Motivo);

                if (resultado is null)
                    return NotFound(new { mensagem = "Solicitação não encontrada." });

                return Ok(resultado);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Fotos de verificação do motorista, servidas como binário — mesmo padrão de
        // CorridasController.ObterFotoMotorista, aqui liberado só pra role Admin (classe toda já exige
        // isso) em vez de checar dono de corrida.
        [HttpGet("motoristas/{motoristaId:guid}/foto-veiculo")]
        public async Task<IActionResult> ObterFotoVeiculo(Guid motoristaId)
            => await ServirFotoAsync(await _motoristaService.ObterCaminhoFotoVeiculoAsync(motoristaId));

        [HttpGet("motoristas/{motoristaId:guid}/foto-placa")]
        public async Task<IActionResult> ObterFotoPlaca(Guid motoristaId)
            => await ServirFotoAsync(await _motoristaService.ObterCaminhoFotoPlacaAsync(motoristaId));

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
