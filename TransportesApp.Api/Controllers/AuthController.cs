using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<Usuario> userManager,
            IConfiguration configuration,
            ClienteService clienteService,
            MotoristaService motoristaService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
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