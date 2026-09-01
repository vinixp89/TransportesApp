using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class PromocaoLancamentoRepository : IPromocaoLancamentoRepository
    {
        private readonly AppDbContext _context;

        public PromocaoLancamentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> ContarAsync()
        {
            return await _context.PromocoesLancamento.CountAsync();
        }

        public async Task<bool> ClienteJaRecebeuAsync(Guid clienteId)
        {
            return await _context.PromocoesLancamento.AnyAsync(p => p.ClienteId == clienteId);
        }

        public async Task AdicionarAsync(PromocaoLancamento promocao)
        {
            await _context.PromocoesLancamento.AddAsync(promocao);
            await _context.SaveChangesAsync();
        }
    }
}
