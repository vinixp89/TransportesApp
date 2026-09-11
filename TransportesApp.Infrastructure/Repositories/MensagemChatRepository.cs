using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class MensagemChatRepository : IMensagemChatRepository
    {
        private readonly AppDbContext _context;

        public MensagemChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MensagemChat>> ListarPorCorridaAsync(Guid corridaId, DateTime? desde)
        {
            var query = _context.MensagensChat.Where(m => m.CorridaId == corridaId);

            if (desde is not null)
                query = query.Where(m => m.DataEnvio > desde.Value);

            return await query.OrderBy(m => m.DataEnvio).ToListAsync();
        }

        public async Task AdicionarAsync(MensagemChat mensagem)
        {
            await _context.MensagensChat.AddAsync(mensagem);
            await _context.SaveChangesAsync();
        }
    }
}
