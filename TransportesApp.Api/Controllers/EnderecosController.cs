using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnderecosController : ControllerBase
    {
        private readonly EnderecoAutocompleteService _enderecoAutocompleteService;

        public EnderecosController(EnderecoAutocompleteService enderecoAutocompleteService)
        {
            _enderecoAutocompleteService = enderecoAutocompleteService;
        }

        // Autocomplete pros campos de origem/destino da corrida — devolve sugestões conforme o
        // cliente digita, sem expor a chave da API de mapas pro navegador.
        [HttpGet("autocompletar")]
        public async Task<IActionResult> Autocompletar([FromQuery] string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Ok(Array.Empty<object>());

            try
            {
                var sugestoes = await _enderecoAutocompleteService.AutocompletarAsync(texto);
                return Ok(sugestoes);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Detalha a sugestão escolhida (place_id devolvido por /autocompletar) em campos estruturados,
        // pra preencher o formulário sem o cliente precisar digitar rua/bairro/cidade/estado à mão.
        [HttpGet("detalhes")]
        public async Task<IActionResult> Detalhes([FromQuery] string placeId)
        {
            if (string.IsNullOrWhiteSpace(placeId))
                return BadRequest(new { mensagem = "Informe o placeId da sugestão selecionada." });

            try
            {
                var detalhes = await _enderecoAutocompleteService.ObterDetalhesAsync(placeId);

                if (detalhes is null)
                    return NotFound(new { mensagem = "Não foi possível obter os detalhes desse endereço." });

                return Ok(detalhes);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
