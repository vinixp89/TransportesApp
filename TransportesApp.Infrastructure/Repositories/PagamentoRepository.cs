using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class PagamentoRepository : IPagamentoRepository
    {
        private readonly AppDbContext _context;

        public PagamentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Pagamento?> ObterPorIdAsync(Guid id)
        {
            return await _context.Pagamentos.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AdicionarAsync(Pagamento pagamento)
        {
            await _context.Pagamentos.AddAsync(pagamento);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Pagamento pagamento)
        {
            _context.Pagamentos.Update(pagamento);
            await _context.SaveChangesAsync();
        }
    }
}
