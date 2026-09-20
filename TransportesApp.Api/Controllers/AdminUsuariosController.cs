using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportesApp.Application.Services;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Api.Controllers
{
    // Estatísticas gerais de usuários pro painel inicial do Admin. "LogadosAgora" é uma aproximação
    // baseada em Usuario.UltimoAcessoEm (atualizado a cada requisição autenticada, ver Program.cs
    // JwtBearerEvents.OnTokenValidated) — conta quem teve alguma requisição nos últimos 5 minutos,
    // já que o JWT em si não tem como saber se o app ainda está aberto.
    [ApiController]
    [Route("api/admin/usuarios")]
    [Authorize(Roles = "Admin")]
    public class AdminUsuariosController : ControllerBase
    {
        private static readonly TimeSpan JanelaLogadoAgora = TimeSpan.FromMinutes(5);

        private readonly ClienteService _clienteService;
        private readonly MotoristaService _motoristaService;
        private readonly UserManager<Usuario> _userManager;

        public AdminUsuariosController(
            ClienteService clienteService,
            MotoristaService motoristaService,
            UserManager<Usuario> userManager)
        {
            _clienteService = clienteService;
            _motoristaService = motoristaService;
            _userManager = userManager;
        }

        [HttpGet("estatisticas")]
        public async Task<IActionResult> Estatisticas()
        {
            var totalClientes = (await _clienteService.ListarAsync()).Count();
            var totalMotoristas = (await _motoristaService.ListarAsync()).Count();

            var limite = DateTime.UtcNow - JanelaLogadoAgora;
            var logadosAgora = await _userManager.Users
                .Where(u => u.UltimoAcessoEm != null && u.UltimoAcessoEm > limite)
                .CountAsync();

            return Ok(new
            {
                totalClientes,
                totalMotoristas,
                totalUsuarios = totalClientes + totalMotoristas,
                logadosAgora
            });
        }
    }
}
