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
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _clienteService;
        private readonly DoacaoService _doacaoService;
        private readonly IWebHostEnvironment _ambiente;

        // Mesmo limite/extensões aceitas de MotoristasController.EnviarFotos.
        private const long TamanhoMaximoFotoBytes = 8 * 1024 * 1024;
        private static readonly HashSet<string> ExtensoesAceitas = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

        public ClientesController(ClienteService clienteService, DoacaoService doacaoService, IWebHostEnvironment ambiente)
        {
            _clienteService = clienteService;
            _doacaoService = doacaoService;
            _ambiente = ambiente;
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarClienteRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")!;

            var cliente = await _clienteService.CriarAsync(request, usuarioId, email);
            return Ok(cliente);
        }

        // Selfie pedida no cadastro — mesmo padrão de MotoristasController.EnviarFotos: salva em
        // disco (fora de qualquer rota estática pública), guarda só o caminho relativo no banco.
        [Authorize(Roles = "Cliente")]
        [HttpPost("foto-selfie")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> EnviarFotoSelfie(IFormFile selfie)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de enviar a selfie." });

            if (selfie is null || selfie.Length == 0)
                return BadRequest(new { mensagem = "A selfie é obrigatória." });

            if (selfie.Length > TamanhoMaximoFotoBytes)
                return BadRequest(new { mensagem = "A selfie passa do limite de 8 MB." });

            if (!ExtensoesAceitas.Contains(Path.GetExtension(selfie.FileName)))
                return BadRequest(new { mensagem = "A selfie precisa ser JPG ou PNG." });

            var pastaCliente = Path.Combine(_ambiente.ContentRootPath, "uploads", "clientes", cliente.Id.ToString());
            Directory.CreateDirectory(pastaCliente);

            var extensao = Path.GetExtension(selfie.FileName).ToLowerInvariant();
            var caminhoCompleto = Path.Combine(pastaCliente, $"selfie{extensao}");

            await using (var stream = System.IO.File.Create(caminhoCompleto))
                await selfie.CopyToAsync(stream);

            var selfieUrl = $"clientes/{cliente.Id}/selfie{extensao}";
            var atualizado = await _clienteService.DefinirFotoSelfieAsync(usuarioId, selfieUrl);

            return Ok(atualizado);
        }

        // Aceite dos termos de uso: ver AuthController.AceitarTermos — endpoint único, compartilhado
        // com Motorista (a role no token decide qual perfil atualizar), não duplicado aqui.

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var clientes = await _clienteService.ListarAsync();
            return Ok(clientes);
        }

        // Busca de destinatário pra doar uma corrida (ver CarteirasController.Doar) — só por e-mail
        // exato, nunca por nome, pra não virar uma lista pesquisável de todos os clientes.
        [Authorize(Roles = "Cliente")]
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { mensagem = "Informe o e-mail do destinatário." });

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var clienteLogado = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (clienteLogado is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de buscar outro cliente." });

            var resultado = await _doacaoService.BuscarPorEmailAsync(email, clienteLogado.Id);

            if (resultado is null)
                return NotFound(new { mensagem = "Nenhum cliente encontrado com esse e-mail." });

            return Ok(resultado);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var cliente = await _clienteService.ObterPorIdAsync(id);

            if (cliente is null)
                return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")!);

                if (cliente.UsuarioId != usuarioId)
                    return Forbid();
            }

            return Ok(cliente);
        }
    }
}