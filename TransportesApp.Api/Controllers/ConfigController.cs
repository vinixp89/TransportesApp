using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Feature flag remota pro app Motorista: a tela de saldo/saque (recurso financeiro) só fica
        // visível quando isso vier true. Existe porque o Google Play exige conta tipo Organização
        // pra apps com recursos financeiros (ver Financial Features Declaration) — enquanto essa
        // verificação não sai, publicamos o app sem esse pedaço, e liberamos depois trocando só a
        // variável de ambiente no servidor (Features__CarteiraMotoristaLiberada), sem precisar de
        // novo build nem passar pela análise da loja de novo. Público de propósito, é só um booleano.
        [AllowAnonymous]
        [HttpGet("app")]
        public IActionResult App()
        {
            var valorConfig = _configuration["Features:CarteiraMotoristaLiberada"];
            var carteiraMotoristaLiberada = bool.TryParse(valorConfig, out var liberada) && liberada;

            return Ok(new { carteiraMotoristaLiberada });
        }
    }
}
