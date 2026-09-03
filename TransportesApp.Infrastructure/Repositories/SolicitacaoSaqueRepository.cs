using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class SolicitacaoSaqueRepository : ISolicitacaoSaqueRepository
    {
        private readonly AppDbContext _context;

        public SolicitacaoSaqueRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SolicitacaoSaque?> ObterPorIdAsync(Guid id)
        {
            return await _context.SolicitacoesSaque.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<SolicitacaoSaque>> ListarPorMotoristaIdAsync(Guid motoristaId)
        {
            return await _context.SolicitacoesSaque
                .Where(s => s.MotoristaId == motoristaId)
                .OrderByDescending(s => s.DataSolicitacao)
                .ToListAsync();
        }

        public async Task<IEnumerable<SolicitacaoSaque>> ListarPendentesAsync()
        {
            return await _context.SolicitacoesSaque
                .Where(s => s.Status == StatusSolicitacaoSaque.Pendente)
                .OrderBy(s => s.DataSolicitacao)
                .ToListAsync();
        }

        public async Task AdicionarAsync(SolicitacaoSaque solicitacao)
        {
            await _context.SolicitacoesSaque.AddAsync(solicitacao);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(SolicitacaoSaque solicitacao)
        {
            _context.SolicitacoesSaque.Update(solicitacao);
            await _context.SaveChangesAsync();
        }
    }
}
