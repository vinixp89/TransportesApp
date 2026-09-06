using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Quantidade precisa ser 3, 5 ou 10 (FaixaDistancia.TamanhosPacoteDisponiveis).
    public record CriarPacoteCorridasRequest(CorFaixa Faixa, int Quantidade);

    public record PacoteCorridasResponse
        (
            Guid Id,
            Guid ClienteId,
            CorFaixa Faixa,
            int QuantidadeTotal,
            int QuantidadeUsada,
            int QuantidadeRestante,
            decimal PrecoPago,
            DateTime DataCompra
        );

    // Catálogo de pacotes disponíveis pra compra — uma entrada por faixa, com o preço calculado
    // pra cada tamanho de pacote (FaixaDistancia.TamanhosPacoteDisponiveis).
    public record TamanhoPacoteResponse(int Quantidade, decimal Preco);

    public record CatalogoPacoteResponse(CorFaixa Faixa, decimal PrecoAvulso, IReadOnlyList<TamanhoPacoteResponse> Tamanhos);

    // Resposta de POST /PacotesCorridas/comprar (ver PacoteCorridasService.IniciarCompraAsync) — o
    // pacote já existe (com Pago=false) mas só fica utilizável depois que o cliente concluir o
    // checkout em CheckoutUrl.
    public record IniciarCompraPacoteResponse(Guid PacoteId, string CheckoutUrl);

    // Mesma ideia, só que pelo Pix direto (ver PacoteCorridasService.IniciarCompraPixAsync).
    public record IniciarCompraPacotePixResponse(Guid PacoteId, string PagamentoGatewayId, string QrCodeCopiaCola, string QrCodeBase64);
}
