using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class TransacaoCarteiraRepository : ITransacaoCarteiraRepository
    {
        private readonly AppDbContext _context;

        public TransacaoCarteiraRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AdicionarAsync(TransacaoCarteira transacao)
        {
            await _context.TransacoesCarteira.AddAsync(transacao);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<TransacaoCarteira>> ListarPorCarteiraIdAsync(Guid carteiraId)
        {
            return await _context.TransacoesCarteira
                .Where(t => t.CarteiraId == carteiraId)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }
    }
}
