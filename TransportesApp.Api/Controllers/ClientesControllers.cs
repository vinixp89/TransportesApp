using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _clienteService;

        public ClientesController(ClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarClienteRequest request)
        {
            var cliente = await _clienteService.CriarAsync(request);
            return Ok(cliente);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var cliente = await _clienteService.ObterPorIdAsync(id);

            if (cliente is null)
                return NotFound();

            return Ok(cliente);
        }
    }
}