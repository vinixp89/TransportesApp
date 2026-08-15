using System;
using System.Collections.Generic;
using System.Text;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Domain.Interfaces
{
    public  interface IClienteRepository
    {
        Task<Cliente?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Cliente cliente);
        Task AtualizarAsync(Cliente cliente);



    }
}
