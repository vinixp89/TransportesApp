using Microsoft.AspNetCore.Mvc;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorridasController : ControllerBase
    {
        private readonly ICorridaRepository _corridaRepository;

        public CorridasController(ICorridaRepository corridaRepository)
        {
            _corridaRepository = corridaRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromQuery] Guid clienteId)
        {
            var origem = new Endereco(
                logradouro: "Rua A",
                numero: "10",
                bairro: "Centro",
                cidade: "Nova Iguaçu",
                estado: "RJ",
                latitude: -22.7556,
                longitude: -43.4603
            );

            var destino = new Endereco(
                logradouro: "Rua B",
                numero: "20",
                bairro: "Centro",
                cidade: "Nova Iguaçu",
                estado: "RJ",
                latitude: -22.7600,
                longitude: -43.4700
            );

            var distanciaEstimadaKm = 8.0;
            var faixa = FaixaDistancia.ClassificarPorDistancia(distanciaEstimadaKm);

            var corrida = new Corrida(
                clienteId: clienteId,
                origem: origem,
                destino: destino,
                distanciaEstimadaKm: distanciaEstimadaKm,
                faixa: faixa,
                tipoConsumo: TipoConsumo.Avulsa,
                pacoteCorridasId: null
            );

            await _corridaRepository.AdicionarAsync(corrida);

            return Ok(corrida);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var corrida = await _corridaRepository.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            return Ok(corrida);
        }
    }
}