using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IMensagemChatRepository
    {
        // "desde" é opcional e serve pro polling incremental do app (só traz mensagem nova desde a
        // última consulta) — sem ele, traz o histórico inteiro da corrida (usado ao abrir o chat).
        Task<IEnumerable<MensagemChat>> ListarPorCorridaAsync(Guid corridaId, DateTime? desde);
        Task AdicionarAsync(MensagemChat mensagem);
    }
}
