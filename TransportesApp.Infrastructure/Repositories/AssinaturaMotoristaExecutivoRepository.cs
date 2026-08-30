using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class AssinaturaMotoristaExecutivoRepository : IAssinaturaMotoristaExecutivoRepository
    {
        private readonly AppDbContext _context;

        public AssinaturaMotoristaExecutivoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AssinaturaMotoristaExecutivo?> ObterAtivaPorMotoristaAsync(Guid motoristaId)
        {
            return await _context.AssinaturasMotoristaExecutivo
                .FirstOrDefaultAsync(a => a.MotoristaId == motoristaId && a.Status == StatusAssinatura.Ativa);
        }

        public async Task<AssinaturaMotoristaExecutivo?> ObterPendentePorMotoristaAsync(Guid motoristaId)
        {
            return await _context.AssinaturasMotoristaExecutivo
                .FirstOrDefaultAsync(a => a.MotoristaId == motoristaId && a.Status == StatusAssinatura.PendentePagamento);
        }

        public async Task<AssinaturaMotoristaExecutivo?> ObterPorIdAsync(Guid id)
        {
            return await _context.AssinaturasMotoristaExecutivo.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AdicionarAsync(AssinaturaMotoristaExecutivo assinatura)
        {
            await _context.AssinaturasMotoristaExecutivo.AddAsync(assinatura);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(AssinaturaMotoristaExecutivo assinatura)
        {
            _context.AssinaturasMotoristaExecutivo.Update(assinatura);
            await _context.SaveChangesAsync();
        }
    }
}
