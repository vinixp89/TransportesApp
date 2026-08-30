using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;
using TransportesApp.Domain.Enums;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CorridasController : ControllerBase
    {
        private readonly CorridaService _corridaService;
        private readonly ClienteService _clienteService;
        private readonly MotoristaService _motoristaService;

        public CorridasController(CorridaService corridaService, ClienteService clienteService, MotoristaService motoristaService)
        {
            _corridaService = corridaService;
            _clienteService = clienteService;
            _motoristaService = motoristaService;
        }

        // Calcula rota/faixa/valor SEM criar a corrida — usado pra tela de confirmação
        // ("essa corrida vai custar X — confirma?") antes do POST que efetivamente solicita.
        [Authorize(Roles = "Cliente")]
        [HttpPost("estimar")]
        public async Task<IActionResult> Estimar([FromBody] EstimarCorridaRequest request)
        {
            try
            {
                var estimativa = await _corridaService.EstimarAsync(request);
                return Ok(estimativa);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarCorridasRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de solicitar corridas." });

            try
            {
                var corrida = await _corridaService.CriarAsync(request, cliente.Id);
                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var corridas = await _corridaService.ListarAsync();
            return Ok(corridas);
        }

        // Histórico completo pro painel de Admin — todas as corridas de todos os clientes e
        // motoristas, já com nome/e-mail do cliente e placa/modelo do motorista resolvidos.
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> ListarParaAdmin()
        {
            var corridas = await _corridaService.ListarParaAdminAsync();
            return Ok(corridas);
        }

        // Extrato de corridas do usuário logado — motorista vê as que dirigiu, cliente vê as que
        // pediu (mais recentes primeiro).
        [Authorize(Roles = "Cliente,Motorista")]
        [HttpGet("minhas")]
        public async Task<IActionResult> ListarMinhas()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            if (User.IsInRole("Motorista"))
            {
                var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

                if (motorista is null)
                    return BadRequest(new { mensagem = "Cadastre-se como motorista antes de ver o extrato de corridas." });

                return Ok(await _corridaService.ListarPorMotoristaAsync(motorista.Id));
            }

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de ver o extrato de corridas." });

            return Ok(await _corridaService.ListarPorClienteAsync(cliente.Id));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await UsuarioParticipaDaCorridaAsync(corrida))
                return Forbid();

            return Ok(corrida);
        }

        // Corridas aguardando motorista, pra ele escolher qual aceitar (ver AtribuirMotorista abaixo).
        [Authorize(Roles = "Motorista")]
        [HttpGet("pendentes")]
        public async Task<IActionResult> ListarPendentes()
        {
            var corridas = await _corridaService.ListarPendentesAsync();
            return Ok(corridas);
        }

        // Corrida em andamento do usuário logado (null se não tiver nenhuma): motorista vê a que
        // aceitou e ainda não finalizou (livre pra aceitar outra em /pendentes se for null);
        // cliente vê a que pediu e ainda não terminou (pro banner "corrida em andamento" no app).
        [Authorize(Roles = "Cliente,Motorista")]
        [HttpGet("atual")]
        public async Task<IActionResult> ObterAtual()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            if (User.IsInRole("Motorista"))
            {
                var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

                if (motorista is null)
                    return BadRequest(new { mensagem = "Cadastre-se como motorista antes de ver suas corridas." });

                return Ok(await _corridaService.ObterAtualDoMotoristaAsync(motorista.Id));
            }

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de ver suas corridas." });

            return Ok(await _corridaService.ObterAtualDoClienteAsync(cliente.Id));
        }

        // Código de confirmação de 4 dígitos — só o cliente dono da corrida vê (é ele quem fala o
        // código de viva voz pro motorista, ver Corrida.IniciarViagem no domínio).
        [Authorize(Roles = "Cliente")]
        [HttpGet("{id:guid}/codigo-confirmacao")]
        public async Task<IActionResult> ObterCodigoConfirmacao(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await ClienteDonoDaCorridaAsync(corrida))
                return Forbid();

            var codigo = await _corridaService.ObterCodigoConfirmacaoAsync(id);
            return Ok(new { codigo });
        }

        // Localização atual do motorista a caminho — pro cliente acompanhar no mapa depois que a
        // corrida é confirmada. Só faz sentido enquanto tem motorista atribuído (Confirmada ou
        // EmAndamento); antes disso ou depois de finalizada/cancelada não há o que mostrar.
        [HttpGet("{id:guid}/localizacao-motorista")]
        public async Task<IActionResult> ObterLocalizacaoMotorista(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await UsuarioParticipaDaCorridaAsync(corrida))
                return Forbid();

            if (corrida.MotoristaId is null || corrida.Status is not (StatusCorrida.Confirmada or StatusCorrida.EmAndamento))
                return BadRequest(new { mensagem = "Essa corrida não tem um motorista a caminho no momento." });

            var motorista = await _motoristaService.ObterPorIdAsync(corrida.MotoristaId.Value);

            if (motorista is null)
                return NotFound();

            return Ok(new LocalizacaoMotoristaResponse(
                motorista.LatitudeAtual,
                motorista.LongitudeAtual,
                motorista.PlacaVeiculo,
                motorista.ModeloVeiculo));
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("{id}/atribuir-motorista")]
        public async Task<IActionResult> AtribuirMotorista(Guid id)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de aceitar corridas." });

            try
            {
                var corrida = await _corridaService.AtribuirMotoristaAsync(id, motorista.Id);

                if (corrida is null)
                    return NotFound();

                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("{id}/iniciar")]
        public async Task<IActionResult> IniciarViagem(Guid id, [FromBody] IniciarViagemRequest request)
        {
            var corridaAtual = await _corridaService.ObterPorIdAsync(id);

            if (corridaAtual is null)
                return NotFound();

            if (!await MotoristaAtribuidoNaCorridaAsync(corridaAtual))
                return Forbid();

            try
            {
                var corrida = await _corridaService.IniciarViagemAsync(id, request.Codigo);

                if (corrida is null)
                    return NotFound();

                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize(Roles = "Motorista")]
        [HttpPatch("{id}/finalizar")]
        public async Task<IActionResult> Finalizar(Guid id, [FromBody] FinalizarCorridaRequest request)
        {
            var corridaAtual = await _corridaService.ObterPorIdAsync(id);

            if (corridaAtual is null)
                return NotFound();

            if (!await MotoristaAtribuidoNaCorridaAsync(corridaAtual))
                return Forbid();

            try
            {
                var resultado = await _corridaService.FinalizarAsync(id, request);

                if (resultado is null)
                    return NotFound();

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid id)
        {
            var corridaAtual = await _corridaService.ObterPorIdAsync(id);

            if (corridaAtual is null)
                return NotFound();

            if (!await UsuarioParticipaDaCorridaAsync(corridaAtual))
                return Forbid();

            try
            {
                // Reembolso automático (devolve o que já foi debitado/consumido) quando quem cancela NÃO
                // é o cliente dono da corrida — motorista atribuído ou Admin cancelando não é culpa do
                // cliente, então ele não deve perder o que já pagou. Cliente cancelando só é reembolsado
                // se a corrida ainda não tinha motorista atribuído (ver Corrida.Cancelar).
                var sempreReembolsar = !await ClienteDonoDaCorridaAsync(corridaAtual);

                var corrida = await _corridaService.CancelarAsync(id, sempreReembolsar);

                if (corrida is null)
                    return NotFound();

                return Ok(corrida);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Admin sempre pode; caso contrário, só o cliente dono da corrida ou o motorista atribuído a ela.
        private async Task<bool> UsuarioParticipaDaCorridaAsync(CorridaResponse corrida)
        {
            if (User.IsInRole("Admin"))
                return true;

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is not null && cliente.Id == corrida.ClienteId)
                return true;

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is not null && corrida.MotoristaId is not null && motorista.Id == corrida.MotoristaId)
                return true;

            return false;
        }

        // true só quando o usuário logado É o cliente dono da corrida (Admin e o motorista atribuído
        // devolvem false) — usado só pra decidir a regra de reembolso no cancelamento (ver Cancelar acima).
        private async Task<bool> ClienteDonoDaCorridaAsync(CorridaResponse corrida)
        {
            if (User.IsInRole("Admin"))
                return false;

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            return cliente is not null && cliente.Id == corrida.ClienteId;
        }

        // Só o motorista atribuído àquela corrida especificamente (não qualquer motorista).
        private async Task<bool> MotoristaAtribuidoNaCorridaAsync(CorridaResponse corrida)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            return motorista is not null && corrida.MotoristaId is not null && motorista.Id == corrida.MotoristaId;
        }
    }
}