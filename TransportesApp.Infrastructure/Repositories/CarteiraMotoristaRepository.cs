using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class CarteiraMotoristaRepository : ICarteiraMotoristaRepository
    {
        private readonly AppDbContext _context;

        public CarteiraMotoristaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CarteiraMotorista?> ObterPorMotoristaIdAsync(Guid motoristaId)
        {
            return await _context.CarteirasMotorista.FirstOrDefaultAsync(c => c.MotoristaId == motoristaId);
        }

        public async Task AdicionarAsync(CarteiraMotorista carteira)
        {
            await _context.CarteirasMotorista.AddAsync(carteira);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(CarteiraMotorista carteira)
        {
            _context.CarteirasMotorista.Update(carteira);
            await _context.SaveChangesAsync();
        }
    }
}
