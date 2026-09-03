using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Email;
using TransportesApp.Application.Services;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ClienteService _clienteService;
        private readonly MotoristaService _motoristaService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthController> _logger;
        private readonly PromocaoLancamentoService _promocaoLancamentoService;

        // Código de redefinição de senha (6 dígitos) guardado em memória por 15 min, junto com o
        // token de verdade do Identity que ele representa — não precisa de tabela nova no banco
        // pra algo tão efêmero, e o app roda numa instância só (sem load balancer).
        private static readonly MemoryCacheEntryOptions CodigoRedefinicaoOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };

        public AuthController(
            UserManager<Usuario> userManager,
            IConfiguration configuration,
            ClienteService clienteService,
            MotoristaService motoristaService,
            IEmailService emailService,
            IMemoryCache cache,
            ILogger<AuthController> logger,
            PromocaoLancamentoService promocaoLancamentoService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _clienteService = clienteService;
            _motoristaService = motoristaService;
            _emailService = emailService;
            _cache = cache;
            _logger = logger;
            _promocaoLancamentoService = promocaoLancamentoService;
        }

        [HttpPost("registrar-cliente")]
        public Task<IActionResult> RegistrarCliente([FromBody] RegistrarClienteRequest request)
            => RegistrarAsync(
                request.Email,
                request.Senha,
                TipoUsuario.Cliente,
                request.Cliente,
                async (dados, usuarioId, email) =>
                {
                    var cliente = await _clienteService.CriarAsync(dados, usuarioId, email);

                    // Promoção de lançamento (ver PromocaoLancamentoService) — nunca deve travar o
                    // cadastro em si, mesmo padrão defensivo do e-mail de boas-vindas logo abaixo.
                    try
                    {
                        await _promocaoLancamentoService.ConcederSeElegivelAsync(cliente.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Falha ao conceder promoção de lançamento pro cliente {ClienteId}", cliente.Id);
                    }
                },
                dados => EmailTemplates.BoasVindasCliente(dados.Nome));

        [HttpPost("registrar-motorista")]
        public Task<IActionResult> RegistrarMotorista([FromBody] RegistrarMotoristaRequest request)
            => RegistrarAsync(
                request.Email,
                request.Senha,
                TipoUsuario.Motorista,
                request.Motorista,
                (dados, usuarioId, email) => _motoristaService.CriarAsync(dados, usuarioId),
                _ => EmailTemplates.BoasVindasMotorista());

        private async Task<IActionResult> RegistrarAsync<TDadosPerfil>(
            string email,
            string senha,
            TipoUsuario tipo,
            TDadosPerfil dadosPerfil,
            Func<TDadosPerfil, Guid, string, Task> criarPerfilAsync,
            Func<TDadosPerfil, EmailMensagem> montarEmailBoasVindas)
        {
            var usuario = new Usuario
            {
                UserName = email,
                Email = email
            };

            var resultado = await _userManager.CreateAsync(usuario, senha);

            if (!resultado.Succeeded)
                return BadRequest(resultado.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(usuario, tipo.ToString());

            try
            {
                await criarPerfilAsync(dadosPerfil, usuario.Id, email);
            }
            catch (ArgumentException ex)
            {
                // Perfil inválido: desfaz o usuário criado pra não deixar um cadastro pela metade.
                await _userManager.DeleteAsync(usuario);
                return BadRequest(new { mensagem = ex.Message });
            }

            try
            {
                var mensagem = montarEmailBoasVindas(dadosPerfil);
                await _emailService.EnviarAsync(usuario.Email!, mensagem.Assunto, mensagem.CorpoHtml);
            }
            catch (Exception ex)
            {
                // Falha no envio do e-mail não deve impedir o cadastro.
                _logger.LogWarning(ex, "Falha ao enviar e-mail de boas-vindas para {Email}", usuario.Email);
            }

            var token = await GerarTokenAsync(usuario);

            return Ok(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _userManager.FindByEmailAsync(request.Email);

            if (usuario is null)
                return Unauthorized(new { mensagem = "Email ou senha inválidos" });

            var senhaValida = await _userManager.CheckPasswordAsync(usuario, request.Senha);

            if (!senhaValida)
                return Unauthorized(new { mensagem = "Email ou senha inválidos" });

            var token = await GerarTokenAsync(usuario);

            return Ok(token);
        }

        // Sempre responde OK, exista o e-mail ou não — evita que alguém descubra quais e-mails
        // têm conta só tentando esse endpoint (enumeração de usuários).
        [HttpPost("esqueci-senha")]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request)
        {
            var usuario = await _userManager.FindByEmailAsync(request.Email);

            if (usuario is not null)
            {
                var codigo = Random.Shared.Next(0, 1_000_000).ToString("D6");
                var tokenIdentity = await _userManager.GeneratePasswordResetTokenAsync(usuario);

                _cache.Set($"redefinir-senha:{request.Email.ToLowerInvariant()}", (codigo, tokenIdentity), CodigoRedefinicaoOptions);

                try
                {
                    var mensagem = EmailTemplates.RedefinicaoSenha(codigo);
                    await _emailService.EnviarAsync(usuario.Email!, mensagem.Assunto, mensagem.CorpoHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar e-mail de redefinição de senha para {Email}", usuario.Email);
                }
            }

            return Ok(new { mensagem = "Se esse e-mail tiver uma conta, enviamos um código de redefinição pra ele." });
        }

        [HttpPost("redefinir-senha")]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest request)
        {
            if (!_cache.TryGetValue($"redefinir-senha:{request.Email.ToLowerInvariant()}", out (string Codigo, string TokenIdentity) pendente)
                || pendente.Codigo != request.Codigo)
                return BadRequest(new { mensagem = "Código inválido ou expirado. Peça um novo." });

            var usuario = await _userManager.FindByEmailAsync(request.Email);

            if (usuario is null)
                return BadRequest(new { mensagem = "Código inválido ou expirado. Peça um novo." });

            var resultado = await _userManager.ResetPasswordAsync(usuario, pendente.TokenIdentity, request.NovaSenha);

            if (!resultado.Succeeded)
                return BadRequest(resultado.Errors.Select(e => e.Description));

            _cache.Remove($"redefinir-senha:{request.Email.ToLowerInvariant()}");

            var token = await GerarTokenAsync(usuario);

            return Ok(token);
        }

        // Exclusão de conta a pedido do cliente (ver tela "Configurações da conta" no app). Anonimiza
        // os dados pessoais do Cliente (ver Cliente.Excluir) e bloqueia o login definitivamente — não
        // apaga a linha do Identity nem os dados financeiros/histórico de corridas (ficam retidos e
        // anonimizados, conforme a política de privacidade).
        [HttpPost("excluir-conta")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> ExcluirConta()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());

            if (usuario is null)
                return NotFound();

            var excluiu = await _clienteService.ExcluirContaAsync(usuarioId);

            if (!excluiu)
                return BadRequest(new { mensagem = "Cadastro de cliente não encontrado pra essa conta." });

            var emailAnonimo = $"excluido-{usuario.Id:N}@vainaboamobilidade.com.br";
            usuario.Email = emailAnonimo;
            usuario.UserName = emailAnonimo;
            usuario.EmailConfirmed = false;
            await _userManager.UpdateAsync(usuario);
            await _userManager.SetLockoutEnabledAsync(usuario, true);
            await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);

            return NoContent();
        }

        private async Task<AuthResponse> GerarTokenAsync(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(usuario);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiraEm = DateTime.UtcNow.AddDays(7);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiraEm,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponse(tokenString, expiraEm, usuario.Email!, usuario.Id);
        }
    }
}