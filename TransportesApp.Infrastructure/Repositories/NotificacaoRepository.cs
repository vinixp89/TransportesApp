using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class NotificacaoRepository : INotificacaoRepository
    {
        private readonly AppDbContext _context;

        public NotificacaoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Notificacao?> ObterPorIdAsync(Guid id)
        {
            return await _context.Notificacoes.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<IEnumerable<Notificacao>> ListarPorClienteAsync(Guid clienteId)
        {
            return await _context.Notificacoes
                .Where(n => n.ClienteId == clienteId)
                .OrderByDescending(n => n.DataCriacao)
                .ToListAsync();
        }

        public async Task<int> ContarNaoLidasPorClienteAsync(Guid clienteId)
        {
            return await _context.Notificacoes.CountAsync(n => n.ClienteId == clienteId && !n.Lida);
        }

        public async Task AdicionarAsync(Notificacao notificacao)
        {
            await _context.Notificacoes.AddAsync(notificacao);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Notificacao notificacao)
        {
            _context.Notificacoes.Update(notificacao);
            await _context.SaveChangesAsync();
        }
    }
}
