using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Enums;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class AvaliacaoRepository : IAvaliacaoRepository
    {
        private readonly AppDbContext _context;

        public AvaliacaoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Avaliacao?> ObterPorCorridaEAutorAsync(Guid corridaId, TipoUsuario autorTipo)
        {
            return await _context.Avaliacoes
                .FirstOrDefaultAsync(a => a.CorridaId == corridaId && a.AutorTipo == autorTipo);
        }

        public async Task<IEnumerable<Avaliacao>> ListarPorCorridaAsync(Guid corridaId)
        {
            return await _context.Avaliacoes
                .Where(a => a.CorridaId == corridaId)
                .ToListAsync();
        }

        public async Task<double?> ObterMediaAsync(Guid avaliadoId)
        {
            // Cast pra double? antes do AverageAsync — assim o SQL AVG() devolve NULL sem linha
            // nenhuma em vez do AverageAsync lançar exceção de "sequência vazia" (comportamento do
            // Average em int/double não anulável).
            return await _context.Avaliacoes
                .Where(a => a.AvaliadoId == avaliadoId)
                .Select(a => (double?)a.Nota)
                .AverageAsync();
        }

        public async Task AdicionarAsync(Avaliacao avaliacao)
        {
            await _context.Avaliacoes.AddAsync(avaliacao);
            await _context.SaveChangesAsync();
        }
    }
}
