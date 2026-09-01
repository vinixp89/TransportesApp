namespace TransportesApp.Application.DTOs
{
    // Info pública da promoção de lançamento (ver PromocaoLancamentoService) — dá pra usar num
    // banner "restam X vagas!" nos apps/site, e também serve pro Admin acompanhar.
    public record PromocaoLancamentoStatusResponse(int Limite, int Concedidas, int VagasRestantes);
}
