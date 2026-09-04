namespace TransportesApp.Domain.Interfaces
{
    // Abstração sobre "qual banco envia o Pix do saque do motorista" — mesma ideia de IGatewayPagamento
    // (cobrança via Mercado Pago), só que pro lado de saída. Hoje só existe InterPagamentoGateway
    // (TransportesApp.Infrastructure/Pagamentos), usando a API Banking do Banco Inter
    // (https://developers.inter.co/references/banking — "Incluir Pagamento Pix").
    public interface IGatewayPagamentoSaque
    {
        // Envia um Pix de verdade pra uma chave. IdIdempotente deve ser único por solicitação (usar o
        // Id da SolicitacaoSaque) — evita pagar duas vezes se a chamada for repetida por retry/erro de
        // rede. Lança exceção se a chamada falhar; quem chama decide o que fazer (ver
        // CarteiraMotoristaService.ConcluirSaqueAsync — não marca a solicitação como concluída se isso
        // lançar).
        Task<PixEnviado> EnviarPixAsync(EnvioPixSolicitado solicitacao);
    }

    public sealed record EnvioPixSolicitado(
        string IdIdempotente,
        decimal Valor,
        string Descricao,
        string ChavePixDestino
    );

    // TipoRetorno vem cru do banco (ex: "APROVACAO" quando a conta exige aprovação manual no Internet
    // Banking antes de executar — ver Aprovar > Gestão de Aprovações). Não traduzimos isso pra um enum
    // nosso porque, do nosso lado, o que importa é só que o banco aceitou a instrução de pagamento;
    // o que acontece depois (aprovação manual ou não) é responsabilidade de quem administra a conta.
    public sealed record PixEnviado(
        string CodigoSolicitacao,
        string TipoRetorno
    );
}
