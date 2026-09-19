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
        private readonly MensagemChatService _mensagemChatService;
        private readonly AvaliacaoService _avaliacaoService;
        private readonly VerificacaoFacialService _verificacaoFacialService;
        private readonly IWebHostEnvironment _ambiente;

        // Mesmo limite/extensões de MotoristasController.EnviarFotos, pra selfie de auditoria tirada
        // ao iniciar/finalizar a corrida.
        private const long TamanhoMaximoFotoVerificacaoBytes = 8 * 1024 * 1024;
        private static readonly HashSet<string> ExtensoesFotoVerificacaoAceitas =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

        public CorridasController(
            CorridaService corridaService,
            ClienteService clienteService,
            MotoristaService motoristaService,
            MensagemChatService mensagemChatService,
            AvaliacaoService avaliacaoService,
            VerificacaoFacialService verificacaoFacialService,
            IWebHostEnvironment ambiente)
        {
            _corridaService = corridaService;
            _clienteService = clienteService;
            _motoristaService = motoristaService;
            _mensagemChatService = mensagemChatService;
            _avaliacaoService = avaliacaoService;
            _verificacaoFacialService = verificacaoFacialService;
            _ambiente = ambiente;
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

        // Só pra corrida por Pacote ou BeneficioPlano (já pagos/liberados antes) — corrida avulsa usa
        // POST /Corridas/avulsa, que abre um pagamento no Mercado Pago em vez de criar na hora.
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

        // Corrida avulsa (sem pacote nem benefício do plano) — abre um pagamento no Mercado Pago pelo
        // valor exato da corrida em vez de debitar de um saldo pré-carregado (a carteira digital foi
        // removida do app Cliente). Devolve a URL de checkout; o app redireciona o cliente pra lá, e a
        // corrida só fica visível pro motorista depois que o pagamento for confirmado (ver
        // PagamentoService.AplicarEfeitoColateralAsync).
        [Authorize(Roles = "Cliente")]
        [HttpPost("avulsa")]
        public async Task<IActionResult> CriarAvulsa([FromBody] CriarCorridasRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de solicitar corridas." });

            try
            {
                var resultado = await _corridaService.IniciarCorridaAvulsaAsync(request, cliente.Id, cliente.Email);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Mesma corrida avulsa de cima, só que pagando via Pix direto (QR Code na hora) em vez de
        // redirecionar pro checkout do Mercado Pago — ver CorridaService.IniciarCorridaAvulsaPixAsync.
        [Authorize(Roles = "Cliente")]
        [HttpPost("avulsa-pix")]
        public async Task<IActionResult> CriarAvulsaPix([FromBody] CriarCorridasRequest request)
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de solicitar corridas." });

            try
            {
                var resultado = await _corridaService.IniciarCorridaAvulsaPixAsync(request, cliente.Id, cliente.Email, cliente.Cpf);
                return Ok(resultado);
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
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return BadRequest(new { mensagem = "Cadastre-se como motorista antes de ver corridas pendentes." });

            var corridas = await _corridaService.ListarPendentesAsync(motorista.Id);
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

        // Dados do motorista atribuído, pro CLIENTE ver durante a corrida (carro/placa/avaliação) —
        // restrito ao cliente dono dessa corrida específica, não é uma concessão permanente entre as
        // duas contas: só dá pra consultar enquanto essa corrida existir com esse motorista atribuído.
        [Authorize(Roles = "Cliente")]
        [HttpGet("{id:guid}/motorista")]
        public async Task<IActionResult> ObterMotoristaDaCorrida(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await ClienteDonoDaCorridaAsync(corrida))
                return Forbid();

            if (corrida.MotoristaId is null)
                return BadRequest(new { mensagem = "Essa corrida ainda não tem motorista atribuído." });

            var motorista = await _motoristaService.ObterPorIdAsync(corrida.MotoristaId.Value);

            if (motorista is null)
                return NotFound();

            return Ok(new MotoristaDaCorridaResponse(
                motorista.Nome,
                motorista.PlacaVeiculo,
                motorista.ModeloVeiculo,
                motorista.AvaliacaoMeida,
                motorista.FotosEnviadas));
        }

        // Espelha ObterMotoristaDaCorrida acima, mas do lado do MOTORISTA: nome e avaliação do
        // cliente atribuído a essa corrida, restrito ao motorista atribuído a ela.
        [Authorize(Roles = "Motorista")]
        [HttpGet("{id:guid}/cliente")]
        public async Task<IActionResult> ObterClienteDaCorrida(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await MotoristaAtribuidoNaCorridaAsync(corrida))
                return Forbid();

            var cliente = await _clienteService.ObterPorIdAsync(corrida.ClienteId);

            if (cliente is null)
                return NotFound();

            return Ok(new ClienteDaCorridaResponse(cliente.Nome, cliente.AvaliacaoMedia));
        }

        // Foto (selfie) do motorista atribuído, servida como binário — mesma autorização do endpoint
        // acima. O arquivo nunca é servido como rota estática pública (ver MotoristasController.
        // SalvarArquivoAsync), só por aqui, atrás de autenticação + checagem de que quem pediu
        // realmente está com uma corrida em andamento com esse motorista.
        [Authorize(Roles = "Cliente")]
        [HttpGet("{id:guid}/motorista/foto")]
        public async Task<IActionResult> ObterFotoMotorista(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await ClienteDonoDaCorridaAsync(corrida))
                return Forbid();

            if (corrida.MotoristaId is null)
                return BadRequest(new { mensagem = "Essa corrida ainda não tem motorista atribuído." });

            var caminhoRelativo = await _motoristaService.ObterCaminhoFotoSelfieAsync(corrida.MotoristaId.Value);

            if (caminhoRelativo is null)
                return NotFound();

            var caminhoCompleto = Path.Combine(_ambiente.ContentRootPath, "uploads", caminhoRelativo);

            if (!System.IO.File.Exists(caminhoCompleto))
                return NotFound();

            var contentType = Path.GetExtension(caminhoCompleto).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                _ => "image/jpeg"
            };

            return PhysicalFile(caminhoCompleto, contentType);
        }

        // Chat entre cliente e motorista — só enquanto a corrida está confirmada (motorista a
        // caminho) ou em andamento; antes de aceita ou depois de finalizada/cancelada não faz
        // sentido conversar. Mesmo padrão de autorização de ObterLocalizacaoMotorista acima.
        [Authorize(Roles = "Cliente,Motorista")]
        [HttpPost("{id:guid}/mensagens")]
        public async Task<IActionResult> EnviarMensagem(Guid id, [FromBody] EnviarMensagemChatRequest request)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await UsuarioParticipaDaCorridaAsync(corrida))
                return Forbid();

            if (corrida.Status is not (StatusCorrida.Confirmada or StatusCorrida.EmAndamento))
                return BadRequest(new { mensagem = "O chat só fica disponível enquanto a corrida está confirmada ou em andamento." });

            var remetenteTipo = User.IsInRole("Cliente") ? TipoUsuario.Cliente : TipoUsuario.Motorista;

            try
            {
                var mensagem = await _mensagemChatService.EnviarAsync(id, remetenteTipo, request.Texto);
                return Ok(mensagem);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // "desde" (opcional, ISO 8601) alimenta o polling incremental do app (só traz mensagem nova) —
        // sem ele, devolve o histórico inteiro da corrida, usado ao abrir o chat pela primeira vez.
        [Authorize(Roles = "Cliente,Motorista")]
        [HttpGet("{id:guid}/mensagens")]
        public async Task<IActionResult> ListarMensagens(Guid id, [FromQuery] DateTime? desde)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await UsuarioParticipaDaCorridaAsync(corrida))
                return Forbid();

            var mensagens = await _mensagemChatService.ListarAsync(id, desde);
            return Ok(mensagens);
        }

        // Avaliação de 1 a 5 estrelas + comentário opcional, só depois da corrida finalizada — cada
        // lado (Cliente sobre o Motorista, Motorista sobre o Cliente) avalia no máximo uma vez (ver
        // AvaliacaoService.AvaliarAsync). Diferente do chat, não usa UsuarioParticipaDaCorridaAsync
        // porque também precisa saber QUEM avaliou (autorTipo) e QUEM foi avaliado (avaliadoId).
        [Authorize(Roles = "Cliente,Motorista")]
        [HttpPost("{id:guid}/avaliar")]
        public async Task<IActionResult> Avaliar(Guid id, [FromBody] AvaliarCorridaRequest request)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (corrida.Status != StatusCorrida.Finalizada)
                return BadRequest(new { mensagem = "Só é possível avaliar corridas finalizadas." });

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            TipoUsuario autorTipo;
            Guid avaliadoId;

            if (User.IsInRole("Cliente"))
            {
                var cliente = await _clienteService.ObterPorUsuarioIdAsync(usuarioId);

                if (cliente is null || cliente.Id != corrida.ClienteId)
                    return Forbid();

                if (corrida.MotoristaId is null)
                    return BadRequest(new { mensagem = "Essa corrida não teve motorista atribuído." });

                autorTipo = TipoUsuario.Cliente;
                avaliadoId = corrida.MotoristaId.Value;
            }
            else
            {
                var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

                if (motorista is null || corrida.MotoristaId != motorista.Id)
                    return Forbid();

                autorTipo = TipoUsuario.Motorista;
                avaliadoId = corrida.ClienteId;
            }

            try
            {
                var avaliacao = await _avaliacaoService.AvaliarAsync(id, autorTipo, avaliadoId, request.Nota, request.Comentario);
                return Ok(avaliacao);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // Devolve as avaliações já feitas dessa corrida (0, 1 ou 2) — usado pela tela pra saber se
        // esconde o formulário de avaliação de quem já avaliou.
        [Authorize(Roles = "Cliente,Motorista")]
        [HttpGet("{id:guid}/avaliacoes")]
        public async Task<IActionResult> ListarAvaliacoes(Guid id)
        {
            var corrida = await _corridaService.ObterPorIdAsync(id);

            if (corrida is null)
                return NotFound();

            if (!await UsuarioParticipaDaCorridaAsync(corrida))
                return Forbid();

            var avaliacoes = await _avaliacaoService.ListarPorCorridaAsync(id);
            return Ok(avaliacoes);
        }

        // Selfie de auditoria (câmera frontal) tirada pelo motorista ao iniciar/finalizar a corrida —
        // comparada contra a selfie do cadastro via AWS Rekognition (ver VerificacaoFacialService).
        // NUNCA bloqueia a viagem: qualquer falha (sem selfie de cadastro, serviço de comparação
        // fora, foto ilegível) é engolida aqui e sempre devolve 200 — o resultado só é visto pelo
        // Admin depois, pra auditoria. Foto salva em disco, nunca servida como rota estática pública
        // (mesmo padrão de MotoristasController.EnviarFotos).
        [Authorize(Roles = "Motorista")]
        [HttpPost("{id:guid}/verificacao-facial")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> RegistrarVerificacaoFacial(Guid id, [FromForm] string momento, [FromForm] IFormFile foto)
        {
            var corridaAtual = await _corridaService.ObterPorIdAsync(id);

            if (corridaAtual is null)
                return NotFound();

            if (!await MotoristaAtribuidoNaCorridaAsync(corridaAtual))
                return Forbid();

            if (!Enum.TryParse<MomentoVerificacaoFacial>(momento, ignoreCase: true, out var momentoEnum))
                return BadRequest(new { mensagem = "Momento inválido — use \"Inicio\" ou \"Fim\"." });

            if (foto is null || foto.Length == 0)
                return BadRequest(new { mensagem = "A foto é obrigatória." });

            if (foto.Length > TamanhoMaximoFotoVerificacaoBytes)
                return BadRequest(new { mensagem = "A foto passa do limite de 8 MB." });

            if (!ExtensoesFotoVerificacaoAceitas.Contains(Path.GetExtension(foto.FileName)))
                return BadRequest(new { mensagem = "A foto precisa ser JPG ou PNG." });

            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            var motorista = await _motoristaService.ObterPorUsuarioIdAsync(usuarioId);

            if (motorista is null)
                return NotFound();

            try
            {
                var pastaMotorista = Path.Combine(_ambiente.ContentRootPath, "uploads", "verificacoes-faciais", motorista.Id.ToString());
                Directory.CreateDirectory(pastaMotorista);

                var extensao = Path.GetExtension(foto.FileName).ToLowerInvariant();
                var nomeArquivo = $"{id}_{momentoEnum}_{DateTime.UtcNow.Ticks}{extensao}";
                var caminhoCompleto = Path.Combine(pastaMotorista, nomeArquivo);

                byte[] fotoCapturadaBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await foto.CopyToAsync(memoryStream);
                    fotoCapturadaBytes = memoryStream.ToArray();
                }

                await System.IO.File.WriteAllBytesAsync(caminhoCompleto, fotoCapturadaBytes);

                var fotoUrlRelativa = $"verificacoes-faciais/{motorista.Id}/{nomeArquivo}";

                byte[]? fotoReferenciaBytes = null;
                var caminhoSelfieCadastro = await _motoristaService.ObterCaminhoFotoSelfieAsync(motorista.Id);

                if (caminhoSelfieCadastro is not null)
                {
                    var caminhoCompletoSelfie = Path.Combine(_ambiente.ContentRootPath, "uploads", caminhoSelfieCadastro);

                    if (System.IO.File.Exists(caminhoCompletoSelfie))
                        fotoReferenciaBytes = await System.IO.File.ReadAllBytesAsync(caminhoCompletoSelfie);
                }

                await _verificacaoFacialService.RegistrarAsync(
                    id, motorista.Id, momentoEnum, fotoUrlRelativa, fotoReferenciaBytes, fotoCapturadaBytes);
            }
            catch
            {
                // Auditoria: qualquer falha aqui (disco, comparação) nunca deve impedir a viagem.
            }

            return Ok();
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