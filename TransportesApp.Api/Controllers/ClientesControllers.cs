using Microsoft.AspNetCore.Mvc;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {

        private readonly IClienteRepository _clienteRepository;

        public ClientesController(IClienteRepository clienteRepository)
        
        {

            _clienteRepository = clienteRepository;
        
        
        }

        [HttpPost]
        public async Task<IActionResult> Criar()
        {

            var endereco = new Endereco
            (
                logradouro: "Rua Das Flores",
                numero: "123",
                bairro: "centro",
                cidade: "Nova Iguaçu",
                estado: "RJ",
                latitude: - 22.7556,
                longitude: -43.4603



            );
            var cliente = new Cliente
            (


                nome: "Cliente Teste",
                telefone: "21999999999",
                email: "teste@exemplo.com",
                endereco: endereco



             );

            await _clienteRepository.AdicionarAsync(cliente);

            return Ok(cliente);
        }

        [HttpGet("{id}")]

        public async Task<IActionResult> ObterPorId(Guid id)
        {

            var cliente = await _clienteRepository.ObterPorIdAsync(id);

            if (cliente is null)
                return NotFound();

            return Ok(cliente);
        
        
        
        }


    }
}
