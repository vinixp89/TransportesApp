namespace TransportesApp.Application.DTOs
{
    // Info pública da promoção de lançamento (ver PromocaoLancamentoService) — dá pra usar num
    // banner "restam X vagas!" nos apps/site, e também serve pro Admin acompanhar.
    public record PromocaoLancamentoStatusResponse(int Limite, int Concedidas, int VagasRestantes);

    // Igual acima, mas pra campanha de 01/10/2026 (ver PromocaoLancamentoService.DataInicioOutubro).
    // "Ativa" já resolve as duas condições pro app não precisar saber a regra: data de início já
    // passou E ainda sobra vaga.
    public record PromocaoOutubroStatusResponse(int Limite, int Concedidas, int VagasRestantes, bool Ativa, DateTime DataInicio);
}
