using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente")]
    public class CarteirasController : ControllerBase
    {
        private readonly CarteiraService _carteiraService;
        private readonly ClienteService _clienteService;

        public CarteirasController(CarteiraService carteiraService, ClienteService clienteService)
        {
            _carteiraService = carteiraService;
            _clienteService = clienteService;
        }

        [HttpGet("minha-carteira")]
        public async Task<IActionResult> MinhaCarteira()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de acessar a carteira." });

            var carteira = await _carteiraService.ObterOuCriarAsync(cliente.Id);
            return Ok(carteira);
        }

        [HttpPost("recarregar")]
        public async Task<IActionResult> Recarregar([FromBody] RecarregarCarteiraRequest request)
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de recarregar a carteira." });

            try
            {
                var carteira = await _carteiraService.RecarregarAsync(cliente.Id, request.Valor);
                return Ok(carteira);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("extrato")]
        public async Task<IActionResult> Extrato()
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar o extrato." });

            var extrato = await _carteiraService.ObterExtratoAsync(cliente.Id);
            return Ok(extrato);
        }

        private async Task<ClienteResponse?> ObterClienteLogadoAsync()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            return await _clienteService.ObterPorUsuarioIdAsync(usuarioId);
        }
    }
}
