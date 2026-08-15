using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public  class CorridaRepository : ICorridaRepository
    {
        private readonly AppDbContext _context;


        public CorridaRepository(AppDbContext context) 
        {

            _context = context;
        
        
        
        }
        public async Task<Corrida?> ObterPorIdAsync(Guid id)
        {
            return await _context.Corridas
            .FirstOrDefaultAsync(c=>c.Id == id);
        
        
        }
        public async Task AdicionarAsync(Corrida corrida)
        {
            await _context.Corridas.AddAsync(corrida);
            await _context.SaveChangesAsync();
        
        
        }

        public async Task AtualizarAsync(Corrida corrida)
        {
            _context.Corridas.Update(corrida);
            await _context.SaveChangesAsync();

        
        
        }

    }
}
