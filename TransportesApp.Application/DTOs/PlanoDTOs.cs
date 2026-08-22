using TransportesApp.Domain.Enums;

namespace TransportesApp.Application.DTOs
{
    // Catálogo dos 3 planos disponíveis, com preço e benefícios — vem do value object
    // PlanoAssinatura, não do banco.
    public record PlanoResponse(TipoPlano Tipo, string Nome, decimal PrecoMensal, IReadOnlyList<string> Beneficios);

    public record AssinarPlanoRequest(TipoPlano Tipo);

    public record AssinaturaPlanoResponse(
        Guid Id,
        TipoPlano Tipo,
        string NomePlano,
        decimal PrecoMensal,
        DateTime DataInicio,
        bool Ativa
    );

    // Status do benefício "1 corrida grátis por mês" da assinatura ativa do cliente — usado pela tela
    // de pedir corrida pra saber se pode oferecer a opção "usar corrida grátis do plano", e pela tela
    // de planos pra mostrar se o benefício já está liberado ou ainda falta uma corrida paga no mês.
    // TemBeneficio vem false (com os demais campos default) quando o cliente não tem assinatura ativa
    // ou o plano dela não inclui corrida grátis.
    public record BeneficioPlanoResponse(
        bool TemBeneficio,
        CorFaixa? CorBeneficio,
        bool Liberado,
        bool JaUsadoNoMes,
        bool DisponivelParaUso
    );
}
