using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class CarteiraRepository : ICarteiraRepository
    {
        private readonly AppDbContext _context;

        public CarteiraRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Carteira?> ObterPorClienteIdAsync(Guid clienteId)
        {
            return await _context.Carteiras.FirstOrDefaultAsync(c => c.ClienteId == clienteId);
        }

        public async Task AdicionarAsync(Carteira carteira)
        {
            await _context.Carteiras.AddAsync(carteira);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Carteira carteira)
        {
            _context.Carteiras.Update(carteira);
            await _context.SaveChangesAsync();
        }
    }
}
