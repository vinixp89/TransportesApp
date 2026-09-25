using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
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

        public async Task<int> ContarAsync(CampanhaPromocional campanha)
        {
            return await _context.PromocoesLancamento.CountAsync(p => p.Campanha == campanha);
        }

        public async Task<bool> ClienteJaRecebeuAsync(Guid clienteId, CampanhaPromocional campanha)
        {
            return await _context.PromocoesLancamento.AnyAsync(p => p.ClienteId == clienteId && p.Campanha == campanha);
        }

        public async Task AdicionarAsync(PromocaoLancamento promocao)
        {
            await _context.PromocoesLancamento.AddAsync(promocao);
            await _context.SaveChangesAsync();
        }
    }
}
