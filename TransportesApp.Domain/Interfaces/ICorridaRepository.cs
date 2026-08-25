using System.Collections.Generic;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public interface ICorridaRepository
    {
        Task<Corrida?> ObterPorIdAsync(Guid id);
        Task<IEnumerable<Corrida>> ListarAsync();
        Task<IEnumerable<Corrida>> ListarPorMotoristaIdAsync(Guid motoristaId);
        Task<IEnumerable<Corrida>> ListarPorClienteIdAsync(Guid clienteId);
        Task AdicionarAsync(Corrida corrida);
        Task AtualizarAsync(Corrida corrida);
    }
}