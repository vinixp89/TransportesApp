using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Latitude/Longitude não são informadas pelo cliente: o backend sempre descobre sozinho a
    // partir do endereço em texto, usando o Geocoding do Google Maps.
    public record EnderecoRequest
        (
            string Logradouro,
            string Numero,
            string Bairro,
            string Cidade,
            string Estado,
            string? Complemento = null
        );

    // A distância não vem mais do cliente — o backend calcula via Google Maps a partir de Origem/Destino,
    // pra evitar que alguém manipule a requisição e informe uma distância falsa pra pagar menos.
    // Categoria vem por último com default Normal — apps já em produção que ainda não mandam esse
    // campo continuam funcionando sem quebrar (System.Text.Json usa o default do construtor quando a
    // propriedade não vem no JSON).
    public record CriarCorridasRequest
        (
            EnderecoRequest Origem,
            EnderecoRequest Destino,
            TipoConsumo TipoConsumo,
            Guid? PacoteCorridasId,
            CategoriaCorrida Categoria = CategoriaCorrida.Normal
        );

    // Resposta de POST /Corridas/avulsa (ver CorridaService.IniciarCorridaAvulsaAsync) — a corrida já
    // existe (em AguardandoPagamento) mas só fica visível pro motorista depois que o cliente concluir
    // o checkout em CheckoutUrl. O app usa CorridaId pra levar o cliente pra tela de acompanhamento
    // assim que voltar do Mercado Pago.
    public record IniciarCorridaAvulsaResponse(Guid CorridaId, string CheckoutUrl);

    public record EnderecoResponse
        (
            string Logradouro,
            string Numero,
            string Bairro,
            string Cidade,
            string Estado,
            double Latitude,
            double Longitude,
            string? Complemento
        );

    public record CorridaResponse
        (
            Guid Id,
            Guid ClienteId,
            Guid? MotoristaId,
            EnderecoResponse Origem,
            EnderecoResponse Destino,
            double DistanciaEstimadaKm,
            double? DistanciaRealKm,
            CorFaixa FaixaContratada,
            CategoriaCorrida Categoria,
            // Valor de tabela da faixa contratada (o mesmo preço fixo, independente de ter sido paga
            // avulsa ou com pacote) — calculado a partir de FaixaDistancia, não é gravado no banco.
            decimal ValorReferencia,
            // Quanto o motorista recebe dessa corrida (ValorReferencia * percentual da plataforma,
            // ver CorridaService.PercentualMotorista) — calculado na hora, não gravado no banco.
            decimal ValorMotorista,
            TipoConsumo TipoConsumo,
            StatusCorrida Status,
            DateTime DataSolicitacao,
            // Só é preenchido na criação da corrida, quando o Google não teve certeza total de algum
            // endereço informado (correspondência parcial). Fica null nas demais consultas.
            IReadOnlyList<string>? AvisosEndereco = null
        );

    // Versão do CorridaResponse pro painel de Admin: troca os IDs de cliente/motorista (que sozinhos
    // não dizem nada numa tela) pelos dados que identificam quem é quem — nome/e-mail do cliente e
    // placa/modelo do motorista (motorista não tem "nome" no domínio, só os dados do veículo).
    public record CorridaAdminResponse
        (
            Guid Id,
            string ClienteNome,
            string ClienteEmail,
            string? MotoristaPlaca,
            string? MotoristaModelo,
            EnderecoResponse Origem,
            EnderecoResponse Destino,
            double DistanciaEstimadaKm,
            double? DistanciaRealKm,
            CorFaixa FaixaContratada,
            CategoriaCorrida Categoria,
            decimal ValorReferencia,
            decimal ValorMotorista,
            TipoConsumo TipoConsumo,
            StatusCorrida Status,
            DateTime DataSolicitacao
        );

    // Localização atual do motorista atribuído à corrida — consultada pelo cliente pra acompanhar
    // o deslocamento no mapa depois que a corrida é confirmada (ver CorridasController).
    public record LocalizacaoMotoristaResponse(
        double? Latitude,
        double? Longitude,
        string PlacaVeiculo,
        string ModeloVeiculo
    );

    public record IniciarViagemRequest(string Codigo);

    public record FinalizarCorridaRequest(double DistanciaReal);

    public record FinalizarCorridaResponse(bool EstouroFaixa,CorridaResponse Corrida);

    // Calcula rota, faixa e valor SEM criar a corrida — usado pra mostrar uma tela de confirmação
    // ("essa corrida vai custar X, faixa Y — confirma?") antes de efetivamente solicitar.
    public record EstimarCorridaRequest(EnderecoRequest Origem, EnderecoRequest Destino, CategoriaCorrida Categoria = CategoriaCorrida.Normal);

    public record EstimarCorridaResponse
        (
            EnderecoResponse Origem,
            EnderecoResponse Destino,
            double DistanciaEstimadaKm,
            double? DuracaoEstimadaMinutos,
            CorFaixa Faixa,
            CategoriaCorrida Categoria,
            decimal ValorReferencia,
            IReadOnlyList<string>? AvisosEndereco
        );
}