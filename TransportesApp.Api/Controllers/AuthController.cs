using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ClienteService _clienteService;
        private readonly MotoristaService _motoristaService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            IConfiguration configuration,
            ClienteService clienteService,
            MotoristaService motoristaService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _clienteService = clienteService;
            _motoristaService = motoristaService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("registrar-cliente")]
        public Task<IActionResult> RegistrarCliente([FromBody] RegistrarClienteRequest request)
            => RegistrarAsync(
                request.Email,
                request.Senha,
                TipoUsuario.Cliente,
                request.Cliente,
                _clienteService.CriarAsync,
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

        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _userManager.FindByEmailAsync(request.Email);

            if (usuario is null)
                return Unauthorized(new { mensagem = "Email ou senha inválidos" });

            // CheckPasswordSignInAsync (em vez de CheckPasswordAsync) é quem aciona o lockout do
            // Identity: incrementa AccessFailedCount a cada erro e bloqueia a conta depois de
            // Lockout.MaxFailedAccessAttempts (ver Program.cs) — essencial contra força bruta.
            var resultado = await _signInManager.CheckPasswordSignInAsync(usuario, request.Senha, lockoutOnFailure: true);

            if (resultado.IsLockedOut)
                return StatusCode(StatusCodes.Status423Locked, new { mensagem = "Conta temporariamente bloqueada por excesso de tentativas. Tente novamente em alguns minutos." });

            if (!resultado.Succeeded)
                return Unauthorized(new { mensagem = "Email ou senha inválidos" });

            var token = await GerarTokenAsync(usuario);

            return Ok(token);
        }

        private async Task<AuthResponse> GerarTokenAsync(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var securityStamp = await _userManager.GetSecurityStampAsync(usuario);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Validado a cada request em Program.cs (JwtBearerEvents.OnTokenValidated) — é o que
                // permite revogar um token antes da expiração (ex: ao trocar senha) mesmo sem refresh token.
                new Claim("security_stamp", securityStamp)
            };

            var roles = await _userManager.GetRolesAsync(usuario);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Reduzido de 7 dias pra 24h: com a checagem de SecurityStamp acima, a revogação (troca de
            // senha, por exemplo) já é imediata — essa janela menor é só defesa em profundidade extra
            // pro caso do token vazar sem a senha ser trocada. Se o app precisar manter sessão mais longa
            // sem pedir login todo dia, o próximo passo é implementar refresh token nos 3 clientes (web,
            // app cliente, app motorista).
            var expiraEm = DateTime.UtcNow.AddHours(24);

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