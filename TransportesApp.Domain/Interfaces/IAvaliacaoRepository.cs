using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;

namespace TransportesApp.Domain.Interfaces
{
    public interface IAvaliacaoRepository
    {
        Task<Avaliacao?> ObterPorCorridaEAutorAsync(Guid corridaId, TipoUsuario autorTipo);
        Task<IEnumerable<Avaliacao>> ListarPorCorridaAsync(Guid corridaId);
        // Média de todas as avaliações recebidas por esse Id (Motorista ou Cliente) — null quando
        // ainda não tem nenhuma (ver AvaliacaoService.AtualizarMediaAsync).
        Task<double?> ObterMediaAsync(Guid avaliadoId);
        Task AdicionarAsync(Avaliacao avaliacao);
    }
}
