using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class MotoristaRepository : IMotoristaRepository
    {
        private readonly AppDbContext _context;


        public MotoristaRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task<Motorista?> ObterPorIdAsync(Guid id)
        {

            return await _context.Motoristas
                .FirstOrDefaultAsync(m=>m.Id ==id);
        }


        public async Task AdicionarAsync(Motorista motorista)
        {
            await _context.Motoristas.AddAsync(motorista);
            await _context.SaveChangesAsync();
                
        
        
        }

        public async Task AtualizarAsync(Motorista motorista)
        {
             _context.Motoristas.Update(motorista);
            await _context.SaveChangesAsync();
        
        
        }


    }
}