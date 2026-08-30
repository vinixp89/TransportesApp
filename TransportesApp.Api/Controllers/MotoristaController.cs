using System.IdentityModel.Tokens.Jwt;
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
    public class MotoristasController : ControllerBase
    {
        private readonly MotoristaService _motoristaService;
        private readonly AssinaturaMotoristaBlackService _assinaturaBlackService;

        public MotoristasController(MotoristaService motoristaService, AssinaturaMotoristaBlackService assinaturaBlackService)
        {
            _motoristaService = motoristaService;
            _assinaturaBlackService = assinaturaBlackService;
        }

        [Authorize(Roles = "Motorista")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarMotoristaRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.CriarAsync(request, usuarioId);
            return Ok(motorista);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var motoristas = await _motoristaService.ListarAsync();
            return Ok(motoristas);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("disponiveis")]
        public async Task<IActionResult> ListarDisponiveis()
        {
            var motoristas = await _motoristaService.ListarDisponiveisAsync();
            return Ok(motoristas);
        }

        // Versão pro cliente: sem CPF/CNH. Se passar latitude/longitude, calcula e ordena por distância;
        // "raioKm" (opcional) filtra só quem está dentro desse raio.
        [Authorize(Roles = "Cliente")]
        [HttpGet("disponiveis-resumo")]
        public async Task<IActionResult> ListarDisponiveisResumo(
            [FromQuery] double? latitude, [FromQuery] double? longitude, [FromQuery] double? raioKm)
        {
            var motoristas = await _motoristaService.ListarDisponiveisResumoAsync(latitude, longitude, raioKm);
            return Ok(motoristas);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var motorista = await _motoristaService.ObterPorIdAsync(id);

            if (motorista is null)
                return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")!);

                if (motorista.UsuarioId != usuarioId)
                    return Forbid();
            }

            return Ok(motorista);
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("ficar-disponivel")]
        public async Task<IActionResult> FicarDisponivel()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.FicarDisponivelAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de ficar disponível." });

            return Ok(motorista);
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("ficar-offline")]
        public async Task<IActionResult> FicarOffline()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.FicarOfflineAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de alterar o status." });

            return Ok(motorista);
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("localizacao")]
        public async Task<IActionResult> AtualizarLocalizacao([FromBody] AtualizarLocalizacaoRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.AtualizarLocalizacaoAsync(usuarioId, request.Latitude, request.Longitude);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de atualizar a localização." });

            return Ok(motorista);
        }

        [Authorize(Roles = "Motorista")]
        [HttpGet("black/assinatura")]
        public async Task<IActionResult> MinhaAssinaturaBlack()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar sua assinatura Black." });

            var assinatura = await _assinaturaBlackService.ObterAtualAsync(motorista.Id);
            return Ok(assinatura);
        }

        [Authorize(Roles = "Motorista")]
        [HttpPost("black/assinar")]
        public async Task<IActionResult> AssinarBlack([FromBody] AssinarBlackRequest request)
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de assinar a categoria Black." });

            var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)!;

            try
            {
                var resultado = await _assinaturaBlackService.AssinarAsync(motorista.Id, request.AnoVeiculo, email);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Motorista")]
        [HttpPost("black/cancelar")]
        public async Task<IActionResult> CancelarBlack()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de cancelar a categoria Black." });

            var cancelou = await _assinaturaBlackService.CancelarAsync(motorista.Id);

            if (!cancelou)
                return BadRequest(new { mensagem = "Você não tem uma assinatura Black ativa ou pendente pra cancelar." });

            return NoContent();
        }

        private async Task<MotoristaResponse?> ObterMotoristaLogadoAsync()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            return await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);
        }
    }
}