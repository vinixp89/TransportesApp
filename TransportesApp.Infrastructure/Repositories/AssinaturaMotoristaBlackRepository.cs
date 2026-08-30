using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class AssinaturaMotoristaBlackRepository : IAssinaturaMotoristaBlackRepository
    {
        private readonly AppDbContext _context;

        public AssinaturaMotoristaBlackRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AssinaturaMotoristaBlack?> ObterAtivaPorMotoristaAsync(Guid motoristaId)
        {
            return await _context.AssinaturasMotoristaBlack
                .FirstOrDefaultAsync(a => a.MotoristaId == motoristaId && a.Status == StatusAssinatura.Ativa);
        }

        public async Task<AssinaturaMotoristaBlack?> ObterPendentePorMotoristaAsync(Guid motoristaId)
        {
            return await _context.AssinaturasMotoristaBlack
                .FirstOrDefaultAsync(a => a.MotoristaId == motoristaId && a.Status == StatusAssinatura.PendentePagamento);
        }

        public async Task<AssinaturaMotoristaBlack?> ObterPorIdAsync(Guid id)
        {
            return await _context.AssinaturasMotoristaBlack.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AdicionarAsync(AssinaturaMotoristaBlack assinatura)
        {
            await _context.AssinaturasMotoristaBlack.AddAsync(assinatura);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(AssinaturaMotoristaBlack assinatura)
        {
            _context.AssinaturasMotoristaBlack.Update(assinatura);
            await _context.SaveChangesAsync();
        }
    }
}
