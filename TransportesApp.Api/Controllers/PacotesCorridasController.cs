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
    public class PacotesCorridasController : ControllerBase
    {
        private readonly PacoteCorridasService _pacoteCorridasService;
        private readonly ClienteService _clienteService;

        public PacotesCorridasController(PacoteCorridasService pacoteCorridasService, ClienteService clienteService)
        {
            _pacoteCorridasService = pacoteCorridasService;
            _clienteService = clienteService;
        }

        // Lista as faixas com o preço avulso e o preço calculado de cada tamanho de pacote —
        // usado pra montar a tela "pacotes disponíveis" antes de comprar. Não depende de cliente
        // cadastrado, só de estar autenticado (herda o [Authorize] da classe).
        [HttpGet("catalogo")]
        public IActionResult Catalogo()
        {
            var catalogo = _pacoteCorridasService.ObterCatalogo();
            return Ok(catalogo);
        }

        // Abre o checkout do Mercado Pago (cartão/boleto) pelo valor do pacote — ele só fica
        // utilizável depois que o pagamento confirmar (ver PagamentoService.AplicarEfeitoColateralAsync).
        [Authorize(Roles = "Cliente")]
        [HttpPost("comprar")]
        public async Task<IActionResult> Comprar([FromBody] CriarPacoteCorridasRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de comprar um pacote." });

            try
            {
                var resultado = await _pacoteCorridasService.IniciarCompraAsync(request, cliente.Id, cliente.Email);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Mesma compra de cima, só que via Pix direto (QR Code na hora).
        [Authorize(Roles = "Cliente")]
        [HttpPost("comprar-pix")]
        public async Task<IActionResult> ComprarPix([FromBody] CriarPacoteCorridasRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de comprar um pacote." });

            try
            {
                var resultado = await _pacoteCorridasService.IniciarCompraPixAsync(request, cliente.Id, cliente.Email, cliente.Cpf);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet("meus-pacotes")]
        public async Task<IActionResult> MeusPacotes()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de consultar pacotes." });

            var pacotes = await _pacoteCorridasService.ListarPorClienteAsync(cliente.Id);
            return Ok(pacotes);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var pacote = await _pacoteCorridasService.ObterPorIdAsync(id);

            if (pacote is null)
                return NotFound();

            return Ok(pacote);
        }
    }
}
