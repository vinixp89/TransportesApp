using Microsoft.Extensions.Configuration;
using TransportesApp.Application.DTOs;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;

namespace TransportesApp.Application.Services
{
    // Orquestra pagamentos de forma genérica (ver Pagamento e IGatewayPagamento) — quem chama isso
    // (hoje só PlanoService) não sabe nada sobre Mercado Pago, só passa "cobre X reais desse cliente
    // pra essa referência" e recebe uma URL de checkout de volta.
    public class PagamentoService
    {
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IAssinaturaPlanoRepository _assinaturaPlanoRepository;
        private readonly IAssinaturaMotoristaBlackRepository _assinaturaMotoristaBlackRepository;
        // Acessados direto (não via CarteiraService) pra evitar dependência circular: CarteiraService já
        // depende de PagamentoService pra iniciar a recarga (ver CarteiraService.IniciarRecargaAsync), então
        // PagamentoService não pode depender de volta de CarteiraService — só dos repositórios em si.
        private readonly ICarteiraRepository _carteiraRepository;
        private readonly ITransacaoCarteiraRepository _transacaoCarteiraRepository;
        private readonly IGatewayPagamento _gateway;
        private readonly IConfiguration _configuration;

        public PagamentoService(
            IPagamentoRepository pagamentoRepository,
            IAssinaturaPlanoRepository assinaturaPlanoRepository,
            IAssinaturaMotoristaBlackRepository assinaturaMotoristaBlackRepository,
            ICarteiraRepository carteiraRepository,
            ITransacaoCarteiraRepository transacaoCarteiraRepository,
            IGatewayPagamento gateway,
            IConfiguration configuration)
        {
            _pagamentoRepository = pagamentoRepository;
            _assinaturaPlanoRepository = assinaturaPlanoRepository;
            _assinaturaMotoristaBlackRepository = assinaturaMotoristaBlackRepository;
            _carteiraRepository = carteiraRepository;
            _transacaoCarteiraRepository = transacaoCarteiraRepository;
            _gateway = gateway;
            _configuration = configuration;
        }

        // clienteId aqui é "quem paga" de forma genérica — quando tipoReferencia é
        // AssinaturaMotoristaBlack, quem chama passa o MotoristaId nesse mesmo parâmetro (Pagamento
        // nasceu pensado só em Cliente, mas o campo guarda qualquer Guid de quem está pagando).

        public async Task<PagamentoIniciadoResultado> IniciarPagamentoAsync(
            Guid clienteId,
            TipoReferenciaPagamento tipoReferencia,
            Guid referenciaId,
            decimal valor,
            string descricao,
            string emailPagador)
        {
            var pagamento = new Pagamento(clienteId, tipoReferencia, referenciaId, valor, descricao);
            await _pagamentoRepository.AdicionarAsync(pagamento);

            // As 3 URLs de retorno (sucesso/pendente/falha) apontam todas pra mesma tela no front —
            // o Mercado Pago sempre acrescenta seus próprios parâmetros (status, payment_id,
            // external_reference) na URL de volta, e é isso que a tela usa pra saber o que mostrar.
            var frontendBase = (_configuration["MercadoPago:UrlRetornoFrontend"] ?? "http://localhost:5173").TrimEnd('/');
            var urlRetorno = $"{frontendBase}/pagamentos/retorno";

            var urlNotificacaoBase = _configuration["MercadoPago:UrlNotificacao"];
            var urlNotificacao = string.IsNullOrWhiteSpace(urlNotificacaoBase)
                ? null
                : $"{urlNotificacaoBase.TrimEnd('/')}/api/Pagamentos/webhook";

            var preferencia = await _gateway.CriarPreferenciaAsync(new SolicitacaoPagamento(
                ExternalReference: pagamento.Id.ToString(),
                Descricao: descricao,
                Valor: valor,
                EmailPagador: emailPagador,
                UrlRetornoSucesso: urlRetorno,
                UrlRetornoPendente: urlRetorno,
                UrlRetornoFalha: urlRetorno,
                UrlNotificacao: urlNotificacao
            ));

            pagamento.RegistrarPreference(preferencia.PreferenceId);
            await _pagamentoRepository.AtualizarAsync(pagamento);

            return new PagamentoIniciadoResultado(pagamento.Id, preferencia.UrlCheckout);
        }

        // Chamado tanto pelo webhook quanto pela sincronização manual (ver PagamentosController) — o
        // parâmetro é sempre o Id do PAGAMENTO dentro do Mercado Pago (não o nosso Pagamento.Id). Nunca
        // confia em status vindo de fora: sempre busca de volta na API do gateway antes de aplicar
        // qualquer mudança, então mesmo que alguém chame isso com um id inventado, o pior que acontece
        // é a consulta ao gateway falhar ou devolver algo que não bate com nenhum Pagamento nosso.
        // clienteIdEsperado só é passado pela sincronização manual (ver PagamentosController) — o
        // webhook em si (chamado pelo próprio Mercado Pago, sem cliente logado) não passa nada. Serve
        // pra um cliente não conseguir sincronizar/ver o pagamento de outro cliente só adivinhando um
        // payment_id: se o pagamento existe mas é de outra pessoa, trata como "não encontrado".
        public async Task<PagamentoStatusResponse?> ProcessarNotificacaoAsync(string pagamentoGatewayId, Guid? clienteIdEsperado = null)
        {
            var statusGateway = await _gateway.ConsultarPagamentoAsync(pagamentoGatewayId);

            if (statusGateway.ExternalReference is null || !Guid.TryParse(statusGateway.ExternalReference, out var pagamentoId))
                return null; // pagamento que não veio da nossa aplicação (ou preference antiga/de teste) — ignora

            var pagamento = await _pagamentoRepository.ObterPorIdAsync(pagamentoId);
            if (pagamento is null)
                return null;

            if (clienteIdEsperado is not null && pagamento.ClienteId != clienteIdEsperado.Value)
                return null;

            // Idempotência: o Mercado Pago pode reenviar a mesma notificação várias vezes — só reage
            // de verdade quando o status realmente mudou.
            if (pagamento.Status != statusGateway.Status)
            {
                pagamento.AtualizarStatus(statusGateway.Status, statusGateway.PagamentoGatewayId);
                await _pagamentoRepository.AtualizarAsync(pagamento);
                await AplicarEfeitoColateralAsync(pagamento);
            }

            return new PagamentoStatusResponse(pagamento.Id, pagamento.Status, pagamento.Valor, pagamento.Descricao);
        }

        // Aplica o que cada tipo de referência precisa fazer quando o pagamento muda de status.
        // AssinaturaPlano e RecargaCarteira já estão implementados — Pacotes/Corridas ainda são
        // liberados na hora da criação, sem um estado "pendente de pagamento"; quando entrarem nesse
        // fluxo, o efeito colateral deles (liberar o pacote, confirmar a corrida) entra aqui do mesmo jeito.
        private async Task AplicarEfeitoColateralAsync(Pagamento pagamento)
        {
            if (pagamento.TipoReferencia == TipoReferenciaPagamento.RecargaCarteira)
            {
                await AplicarEfeitoRecargaCarteiraAsync(pagamento);
                return;
            }

            if (pagamento.TipoReferencia == TipoReferenciaPagamento.AssinaturaMotoristaBlack)
            {
                await AplicarEfeitoAssinaturaMotoristaBlackAsync(pagamento);
                return;
            }

            if (pagamento.TipoReferencia != TipoReferenciaPagamento.AssinaturaPlano)
                return;

            var assinatura = await _assinaturaPlanoRepository.ObterPorIdAsync(pagamento.ReferenciaId);

            // Se a assinatura já não está mais PendentePagamento (ex: o cliente cancelou, ou uma
            // tentativa mais nova já tomou o lugar dela), essa confirmação chegou atrasada demais —
            // não mexe em nada, só evita derrubar a aplicação tentando ativar algo que não faz mais sentido.
            if (assinatura is null || assinatura.Status != StatusAssinatura.PendentePagamento)
                return;

            if (pagamento.Status == StatusPagamento.Aprovado)
            {
                assinatura.Ativar();
                await _assinaturaPlanoRepository.AtualizarAsync(assinatura);
            }
            else if (pagamento.Status is StatusPagamento.Recusado or StatusPagamento.Cancelado)
            {
                assinatura.MarcarPagamentoRecusado();
                await _assinaturaPlanoRepository.AtualizarAsync(assinatura);
            }
            // EmProcessamento/Estornado não mexem na assinatura aqui: EmProcessamento ainda pode virar
            // Aprovado ou Recusado depois, e Estornado (reembolso de algo já aprovado) fica fora do
            // escopo desta primeira versão — a assinatura continua ativa até ser cancelada manualmente.
        }

        // Mesma lógica do branch AssinaturaPlano acima, só que pra assinatura Black do motorista —
        // ver AssinaturaMotoristaBlackService.AssinarAsync, que é quem cria o Pagamento com esse tipo.
        private async Task AplicarEfeitoAssinaturaMotoristaBlackAsync(Pagamento pagamento)
        {
            var assinatura = await _assinaturaMotoristaBlackRepository.ObterPorIdAsync(pagamento.ReferenciaId);

            if (assinatura is null || assinatura.Status != StatusAssinatura.PendentePagamento)
                return;

            if (pagamento.Status == StatusPagamento.Aprovado)
            {
                assinatura.Ativar();
                await _assinaturaMotoristaBlackRepository.AtualizarAsync(assinatura);
            }
            else if (pagamento.Status is StatusPagamento.Recusado or StatusPagamento.Cancelado)
            {
                assinatura.MarcarPagamentoRecusado();
                await _assinaturaMotoristaBlackRepository.AtualizarAsync(assinatura);
            }
        }

        // Credita o saldo da carteira só quando o pagamento é aprovado — Recusado/Cancelado nunca
        // creditaram nada, então não tem o que desfazer. EmProcessamento também não faz nada ainda
        // (pode virar Aprovado ou Recusado depois). ReferenciaId aqui é o Id da CARTEIRA (não do
        // cliente) — ver CarteiraService.IniciarRecargaAsync, que passa carteira.Id como referência.
        private async Task AplicarEfeitoRecargaCarteiraAsync(Pagamento pagamento)
        {
            if (pagamento.Status != StatusPagamento.Aprovado)
                return;

            var carteira = await _carteiraRepository.ObterPorIdAsync(pagamento.ReferenciaId);

            // Não deveria acontecer (a carteira é criada antes de iniciar o pagamento), mas não custa
            // proteger em vez de derrubar a aplicação numa notificação atrasada/inconsistente.
            if (carteira is null)
                return;

            carteira.Recarregar(pagamento.Valor);
            await _carteiraRepository.AtualizarAsync(carteira);

            var transacao = new TransacaoCarteira(carteira.Id, TipoTransacaoCarteira.Recarga, pagamento.Valor, "Recarga via Mercado Pago");
            await _transacaoCarteiraRepository.AdicionarAsync(transacao);
        }
    }
}
