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
}
