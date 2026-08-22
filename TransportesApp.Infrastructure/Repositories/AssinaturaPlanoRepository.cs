using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class AssinaturaPlanoRepository : IAssinaturaPlanoRepository
    {
        private readonly AppDbContext _context;

        public AssinaturaPlanoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AssinaturaPlano?> ObterAtivaPorClienteAsync(Guid clienteId)
        {
            return await _context.AssinaturasPlano
                .FirstOrDefaultAsync(a => a.ClienteId == clienteId && a.Status == StatusAssinatura.Ativa);
        }

        public async Task<AssinaturaPlano?> ObterPendentePorClienteAsync(Guid clienteId)
        {
            return await _context.AssinaturasPlano
                .FirstOrDefaultAsync(a => a.ClienteId == clienteId && a.Status == StatusAssinatura.PendentePagamento);
        }

        public async Task<AssinaturaPlano?> ObterPorIdAsync(Guid id)
        {
            return await _context.AssinaturasPlano.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<AssinaturaPlano>> ListarPorClienteAsync(Guid clienteId)
        {
            return await _context.AssinaturasPlano
                .Where(a => a.ClienteId == clienteId)
                .OrderByDescending(a => a.DataInicio)
                .ToListAsync();
        }

        public async Task AdicionarAsync(AssinaturaPlano assinatura)
        {
            await _context.AssinaturasPlano.AddAsync(assinatura);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(AssinaturaPlano assinatura)
        {
            _context.AssinaturasPlano.Update(assinatura);
            await _context.SaveChangesAsync();
        }
    }
}
