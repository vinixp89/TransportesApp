using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class VerificacaoFacialRepository : IVerificacaoFacialRepository
    {
        private readonly AppDbContext _context;

        public VerificacaoFacialRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AdicionarAsync(VerificacaoFacial verificacao)
        {
            await _context.VerificacoesFaciais.AddAsync(verificacao);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(VerificacaoFacial verificacao)
        {
            _context.VerificacoesFaciais.Update(verificacao);
            await _context.SaveChangesAsync();
        }

        public async Task<VerificacaoFacial?> ObterPorIdAsync(Guid id)
        {
            return await _context.VerificacoesFaciais.FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<IEnumerable<VerificacaoFacial>> ListarPorCorridaAsync(Guid corridaId)
        {
            return await _context.VerificacoesFaciais
                .Where(v => v.CorridaId == corridaId)
                .ToListAsync();
        }

        public async Task<IEnumerable<VerificacaoFacial>> ListarRecentesAsync(int quantidade)
        {
            return await _context.VerificacoesFaciais
                .OrderByDescending(v => v.DataHora)
                .Take(quantidade)
                .ToListAsync();
        }
    }
}
