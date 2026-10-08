namespace TransportesApp.Application.DTOs
{
    // Visão do Admin da promoção de boas-vindas do motorista (ver BonusMotoristaService) — o app
    // Motorista não recebe esses números de propósito (vagas restantes ficam ocultas pra ele).
    public record BonusMotoristaStatusResponse(
        int LimiteVagas,
        decimal ValorBonus,
        int VagasReservadas,
        int VagasRestantes,
        int BonusLiberados);

    // Uma linha da lista do Admin: quem pegou vaga e se já finalizou a 1ª corrida (Liberado).
    public record BonusMotoristaItemResponse(
        Guid MotoristaId,
        string Nome,
        string Telefone,
        decimal Valor,
        DateTime DataConcedido,
        bool Liberado,
        DateTime? DataLiberacao);
}
