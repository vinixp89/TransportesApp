using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportesApp.Application.DTOs;
using TransportesApp.Application.Services;

namespace TransportesApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagamentosController : ControllerBase
    {
        private readonly PagamentoService _pagamentoService;
        private readonly ClienteService _clienteService;

        public PagamentosController(PagamentoService pagamentoService, ClienteService clienteService)
        {
            _pagamentoService = pagamentoService;
            _clienteService = clienteService;
        }

        // Mercado Pago chama essa rota direto do servidor deles — não tem usuário logado nem token
        // nosso aqui, por isso ela fica sem [Authorize] (diferente do resto da API). A legitimidade não
        // vem de autenticação nenhuma: vem de a gente nunca confiar no corpo da notificação e sempre
        // consultar o pagamento de volta na API do Mercado Pago usando nosso próprio Access Token —
        // ver PagamentoService.ProcessarNotificacaoAsync.
        //
        // O Mercado Pago manda o tipo/id tanto via query string (formato mais novo) quanto no corpo —
        // aceita os dois. Referência: https://www.mercadopago.com.br/developers/pt/docs/checkout-pro/additional-content/notifications/webhooks
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook(
            [FromQuery(Name = "type")] string? tipoQuery,
            [FromQuery(Name = "data.id")] string? idQuery,
            [FromBody] WebhookPayload? payload)
        {
            var tipo = tipoQuery ?? payload?.Type;
            var id = idQuery ?? payload?.Data?.Id;

            // Notificação de outro tipo (ex: merchant_order) — não é erro, só não é o que a gente
            // processa. Sempre 200 aqui: devolver erro faria o Mercado Pago reenviar à toa.
            if (tipo != "payment" || string.IsNullOrWhiteSpace(id))
                return Ok();

            try
            {
                await _pagamentoService.ProcessarNotificacaoAsync(id);
            }
            catch
            {
                // Falha de verdade (rede, gateway fora do ar) — devolve erro de propósito, assim o
                // Mercado Pago reenvia essa notificação depois (ele tem retry automático com backoff).
                return StatusCode(500);
            }

            return Ok();
        }

        // Sincronização manual: a tela de retorno do pagamento chama isso assim que volta do Mercado
        // Pago, passando o payment_id que veio na URL de retorno. Principal utilidade é desenvolvimento
        // local — sem uma URL pública configurada (MercadoPago:UrlNotificacao), o webhook nunca é
        // registrado, então é essa chamada que efetivamente confirma o pagamento. Em produção também
        // serve de rede de segurança caso o webhook atrase.
        [HttpPost("sincronizar/{pagamentoGatewayId}")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Sincronizar(string pagamentoGatewayId)
        {
            var cliente = await ObterClienteLogadoAsync();

            if (cliente is null)
                return BadRequest(new { mensagem = "Cadastre-se como cliente antes de sincronizar um pagamento." });

            var status = await _pagamentoService.ProcessarNotificacaoAsync(pagamentoGatewayId, cliente.Id);

            if (status is null)
                return NotFound(new { mensagem = "Pagamento não encontrado." });

            return Ok(status);
        }

        private async Task<ClienteResponse?> ObterClienteLogadoAsync()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

            return await _clienteService.ObterPorUsuarioIdAsync(usuarioId);
        }
    }
}
