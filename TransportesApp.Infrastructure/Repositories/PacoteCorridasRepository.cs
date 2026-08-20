using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class PacoteCorridasRepository : IPacoteCorridasRepository
    {
        private readonly AppDbContext _context;

        public PacoteCorridasRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PacoteCorridas?> ObterPorIdAsync(Guid id)
        {
            return await _context.PacotesCorridas.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<PacoteCorridas>> ListarPorClienteAsync(Guid clienteId)
        {
            return await _context.PacotesCorridas
                .Where(p => p.ClienteId == clienteId)
                .ToListAsync();
        }

        public async Task AdicionarAsync(PacoteCorridas pacote)
        {
            await _context.PacotesCorridas.AddAsync(pacote);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(PacoteCorridas pacote)
        {
            _context.PacotesCorridas.Update(pacote);
            await _context.SaveChangesAsync();
        }
    }
}
