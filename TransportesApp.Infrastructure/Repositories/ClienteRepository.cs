using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class ClienteRepository : IClienteRepository
    {

        private readonly AppDbContext _context;
        public ClienteRepository(AppDbContext context) 
        {

            _context = context;
        
        }

        public async Task<Cliente?> ObterPorIdAsync(Guid id)
        {

            return await _context.Clientes
                  .FirstOrDefaultAsync(c => c.Id == id);

        }

        public async Task<Cliente?> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        }

        public async Task<IEnumerable<Cliente>> ListarAsync()
        {
            return await _context.Clientes.ToListAsync();
        }

        public async Task AdicionarAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();
        
        
        }
        public async Task AtualizarAsync(Cliente cliente) 

        {

            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();


        
        }
    }
}
