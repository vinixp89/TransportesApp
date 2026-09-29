using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class PushTokenRepository : IPushTokenRepository
    {
        private readonly AppDbContext _context;

        public PushTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PushToken?> ObterPorTokenAsync(string token)
        {
            return await _context.PushTokens.FirstOrDefaultAsync(p => p.Token == token);
        }

        public async Task AdicionarAsync(PushToken pushToken)
        {
            await _context.PushTokens.AddAsync(pushToken);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(PushToken pushToken)
        {
            _context.PushTokens.Update(pushToken);
            await _context.SaveChangesAsync();
        }

        public async Task RemoverAsync(string token)
        {
            var existente = await ObterPorTokenAsync(token);
            if (existente is null)
                return;

            _context.PushTokens.Remove(existente);
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<string>> ListarTokensPorPapelAsync(TipoUsuario papel)
        {
            return await _context.PushTokens
                .Where(p => p.Papel == papel)
                .Select(p => p.Token)
                .ToListAsync();
        }
    }
}
