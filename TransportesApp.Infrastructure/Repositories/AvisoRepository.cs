using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class AvisoRepository : IAvisoRepository
    {
        private readonly AppDbContext _context;

        public AvisoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Aviso?> ObterAtivoAsync(DateTime agora)
        {
            return await _context.Avisos
                .Where(a => a.Ativo && a.DataInicio <= agora && a.DataFim >= agora)
                .OrderByDescending(a => a.DataInicio)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Aviso>> ListarAsync()
        {
            return await _context.Avisos
                .OrderByDescending(a => a.DataInicio)
                .ToListAsync();
        }

        public async Task<Aviso?> ObterPorIdAsync(Guid id)
        {
            return await _context.Avisos.FindAsync(id);
        }

        public async Task AdicionarAsync(Aviso aviso)
        {
            await _context.Avisos.AddAsync(aviso);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Aviso aviso)
        {
            _context.Avisos.Update(aviso);
            await _context.SaveChangesAsync();
        }

        public async Task RemoverAsync(Aviso aviso)
        {
            _context.Avisos.Remove(aviso);
            await _context.SaveChangesAsync();
        }
    }
}
