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

        // "Pendente" aqui cobre as duas fases de uma tentativa ainda não resolvida — aguardando
        // aprovação do Admin OU já aprovada e aguardando o motorista pagar — nunca as duas ao mesmo
        // tempo (índice único cobre isso, ver AssinaturaMotoristaExecutivoConfiguration).
        public async Task<AssinaturaMotoristaExecutivo?> ObterPendentePorMotoristaAsync(Guid motoristaId)
        {
            return await _context.AssinaturasMotoristaExecutivo
                .FirstOrDefaultAsync(a => a.MotoristaId == motoristaId
                    && (a.Status == StatusAssinatura.PendentePagamento || a.Status == StatusAssinatura.AguardandoAprovacao));
        }

        public async Task<AssinaturaMotoristaExecutivo?> ObterPorIdAsync(Guid id)
        {
            return await _context.AssinaturasMotoristaExecutivo.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<AssinaturaMotoristaExecutivo>> ListarAguardandoAprovacaoAsync()
        {
            return await _context.AssinaturasMotoristaExecutivo
                .Where(a => a.Status == StatusAssinatura.AguardandoAprovacao)
                .OrderBy(a => a.DataInicio)
                .ToListAsync();
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
