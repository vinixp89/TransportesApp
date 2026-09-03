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
        private readonly AssinaturaMotoristaExecutivoService _assinaturaExecutivoService;
        private readonly IWebHostEnvironment _ambiente;

        // Tamanho máximo por foto e extensões aceitas — autodeclarado, sem verificação de conteúdo
        // real da imagem (nenhuma outra foto/documento no sistema tem essa verificação hoje).
        private const long TamanhoMaximoFotoBytes = 8 * 1024 * 1024;
        private static readonly HashSet<string> ExtensoesAceitas = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

        public MotoristasController(
            MotoristaService motoristaService,
            AssinaturaMotoristaExecutivoService assinaturaExecutivoService,
            IWebHostEnvironment ambiente)
        {
            _motoristaService = motoristaService;
            _assinaturaExecutivoService = assinaturaExecutivoService;
            _ambiente = ambiente;
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

        // Fotos de verificação (selfie, veículo, placa) pedidas depois do cadastro — sempre as 3
        // juntas, numa única tela do app. Salva em disco (não no banco) dentro de uma pasta por
        // motorista, pra nunca misturar arquivo de conta diferente.
        [Authorize(Roles = "Motorista")]
        [HttpPost("fotos")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> EnviarFotos(IFormFile selfie, IFormFile fotoVeiculo, IFormFile fotoPlaca)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de enviar fotos." });

            foreach (var (nome, arquivo) in new[] { ("selfie", selfie), ("fotoVeiculo", fotoVeiculo), ("fotoPlaca", fotoPlaca) })
            {
                if (arquivo is null || arquivo.Length == 0)
                    return BadRequest(new { mensagem = $"A foto \"{nome}\" é obrigatória." });

                if (arquivo.Length > TamanhoMaximoFotoBytes)
                    return BadRequest(new { mensagem = $"A foto \"{nome}\" passa do limite de 8 MB." });

                if (!ExtensoesAceitas.Contains(Path.GetExtension(arquivo.FileName)))
                    return BadRequest(new { mensagem = $"A foto \"{nome}\" precisa ser JPG ou PNG." });
            }

            var pastaMotorista = Path.Combine(_ambiente.ContentRootPath, "uploads", "motoristas", motorista.Id.ToString());
            Directory.CreateDirectory(pastaMotorista);

            var selfieUrl = await SalvarArquivoAsync(selfie, pastaMotorista, "selfie", motorista.Id);
            var veiculoUrl = await SalvarArquivoAsync(fotoVeiculo, pastaMotorista, "veiculo", motorista.Id);
            var placaUrl = await SalvarArquivoAsync(fotoPlaca, pastaMotorista, "placa", motorista.Id);

            var atualizado = await _motoristaService.DefinirFotosAsync(usuarioId, selfieUrl, veiculoUrl, placaUrl);
            return Ok(atualizado);
        }

        private static async Task<string> SalvarArquivoAsync(IFormFile arquivo, string pastaDestino, string nomeBase, Guid motoristaId)
        {
            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            var nomeArquivo = $"{nomeBase}{extensao}";
            var caminhoCompleto = Path.Combine(pastaDestino, nomeArquivo);

            await using (var stream = System.IO.File.Create(caminhoCompleto))
                await arquivo.CopyToAsync(stream);

            // Só um identificador relativo, guardado no banco — de propósito NÃO fica exposto como
            // arquivo estático público (são documentos de identificação: selfie e placa do veículo),
            // então não tem rota nenhuma servindo esse caminho publicamente. Se um dia precisar de
            // revisão (ex: painel do Admin), a leitura tem que passar por um endpoint autenticado que
            // valide a role antes de devolver o arquivo.
            return $"motoristas/{motoristaId}/{nomeArquivo}";
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
        [HttpGet("executivo/assinatura")]
        public async Task<IActionResult> MinhaAssinaturaExecutivo()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de consultar sua assinatura Executivo." });

            var assinatura = await _assinaturaExecutivoService.ObterAtualAsync(motorista.Id);
            return Ok(assinatura);
        }

        [Authorize(Roles = "Motorista")]
        [HttpPost("executivo/assinar")]
        public async Task<IActionResult> AssinarExecutivo([FromBody] AssinarExecutivoRequest request)
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de assinar a categoria Executivo." });

            var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)!;

            try
            {
                var resultado = await _assinaturaExecutivoService.AssinarAsync(motorista.Id, request.AnoVeiculo, email);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Motorista")]
        [HttpPost("executivo/cancelar")]
        public async Task<IActionResult> CancelarExecutivo()
        {
            var motorista = await ObterMotoristaLogadoAsync();

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de cancelar a categoria Executivo." });

            var cancelou = await _assinaturaExecutivoService.CancelarAsync(motorista.Id);

            if (!cancelou)
                return BadRequest(new { mensagem = "Você não tem uma assinatura Executivo ativa ou pendente pra cancelar." });

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