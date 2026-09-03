using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class TransacaoCarteiraMotoristaRepository : ITransacaoCarteiraMotoristaRepository
    {
        private readonly AppDbContext _context;

        public TransacaoCarteiraMotoristaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AdicionarAsync(TransacaoCarteiraMotorista transacao)
        {
            await _context.TransacoesCarteiraMotorista.AddAsync(transacao);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<TransacaoCarteiraMotorista>> ListarPorCarteiraIdAsync(Guid carteiraMotoristaId)
        {
            return await _context.TransacoesCarteiraMotorista
                .Where(t => t.CarteiraMotoristaId == carteiraMotoristaId)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }
    }
}
