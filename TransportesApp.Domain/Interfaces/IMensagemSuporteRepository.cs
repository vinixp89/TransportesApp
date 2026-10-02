using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface IMensagemSuporteRepository
    {
        // "desde" é opcional e serve pro polling incremental do app (só traz mensagem nova desde a
        // última consulta) — sem ele, traz a conversa inteira desse usuário (usado ao abrir o chat).
        Task<IEnumerable<MensagemSuporte>> ListarPorUsuarioAsync(Guid usuarioId, DateTime? desde);

        // Última mensagem de um usuário específico — usado pelo Admin pra saber o TipoUsuario (Cliente
        // ou Motorista) da conversa antes de responder, já que isso não vem em lugar nenhum além das
        // próprias mensagens.
        Task<MensagemSuporte?> ObterUltimaPorUsuarioAsync(Guid usuarioId);

        // Uma por conversa (UsuarioId), pra montar a lista de conversas do Admin — volume de
        // mensagens de suporte é baixo, então agrupar tudo em memória aqui não é um problema de
        // performance (mesmo raciocínio já usado pra buscar o motorista de cada SolicitacaoSaque).
        Task<IEnumerable<MensagemSuporte>> ListarUltimaMensagemDeCadaConversaAsync();

        Task AdicionarAsync(MensagemSuporte mensagem);
    }
}
