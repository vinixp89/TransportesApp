using System.Collections.Generic;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public  interface IClienteRepository
    {
        Task<Cliente?> ObterPorIdAsync(Guid id);
        Task<Cliente?> ObterPorUsuarioIdAsync(Guid usuarioId);
        Task<Cliente?> ObterPorEmailAsync(string email);
        Task<IEnumerable<Cliente>> ListarAsync();
        Task AdicionarAsync(Cliente cliente);
        Task AtualizarAsync(Cliente cliente);
    }
}
