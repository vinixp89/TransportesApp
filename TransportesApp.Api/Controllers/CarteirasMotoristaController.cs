using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Motorista")]
    public class CarteirasMotoristaController : ControllerBase
    {
        private readonly CarteiraMotoristaService _carteiraMotoristaService;
        private readonly MotoristaService _motoristaService;

        public CarteirasMotoristaController(CarteiraMotoristaService carteiraMotoristaService, MotoristaService motoristaService)
        {
            _carteiraMotoristaService = carteiraMotoristaService;
            _motoristaService = motoristaService;
        }

        [HttpGet("minha-carteira")]
        public async Task<IActionResult> MinhaCarteira()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de acessar seu saldo." });

            var carteira = await _carteiraMotoristaService.ObterOuCriarAsync(motorista.Id);
            return Ok(carteira);
        }

        [HttpGet("extrato")]
        public async Task<IActionResult> Extrato()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar o extrato." });

            var extrato = await _carteiraMotoristaService.ObterExtratoAsync(motorista.Id);
            return Ok(extrato);
        }

        [HttpPost("saque")]
        public async Task<IActionResult> SolicitarSaque([FromBody] SolicitarSaqueRequest request)
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de solicitar um saque." });

            try
            {
                var resultado = await _carteiraMotoristaService.SolicitarSaqueAsync(motorista.Id, request);
                return Ok(resultado);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("saques")]
        public async Task<IActionResult> MeusSaques()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar seus saques." });

            var saques = await _carteiraMotoristaService.ListarMinhasSolicitacoesAsync(motorista.Id);
            return Ok(saques);
        }

        private async Task<MotoristaResponse?> ObterMotoristaLogadoAsync()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            return await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);
        }
    }
}
