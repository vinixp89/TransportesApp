using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class MensagemSuporteRepository : IMensagemSuporteRepository
    {
        private readonly AppDbContext _context;

        public MensagemSuporteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MensagemSuporte>> ListarPorUsuarioAsync(Guid usuarioId, DateTime? desde)
        {
            var query = _context.MensagensSuporte.Where(m => m.UsuarioId == usuarioId);

            if (desde is not null)
                query = query.Where(m => m.DataEnvio > desde.Value);

            return await query.OrderBy(m => m.DataEnvio).ToListAsync();
        }

        public async Task<MensagemSuporte?> ObterUltimaPorUsuarioAsync(Guid usuarioId)
        {
            return await _context.MensagensSuporte
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.DataEnvio)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<MensagemSuporte>> ListarUltimaMensagemDeCadaConversaAsync()
        {
            // Agrupado em memória (ver justificativa na interface) em vez de GroupBy+First no banco,
            // que o EF Core nem sempre traduz de forma confiável pra SQL com Npgsql.
            var todas = await _context.MensagensSuporte.OrderBy(m => m.DataEnvio).ToListAsync();

            return todas
                .GroupBy(m => m.UsuarioId)
                .Select(g => g.Last())
                .OrderByDescending(m => m.DataEnvio);
        }

        public async Task AdicionarAsync(MensagemSuporte mensagem)
        {
            await _context.MensagensSuporte.AddAsync(mensagem);
            await _context.SaveChangesAsync();
        }
    }
}
