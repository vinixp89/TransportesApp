using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;

namespace TransportesApp.Infrastructure.Repositories
{
    public class BonusMotoristaRepository : IBonusMotoristaRepository
    {
        private readonly AppDbContext _context;

        public BonusMotoristaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> ContarAsync()
        {
            return await _context.BonusMotoristas.CountAsync();
        }

        public async Task<int> ContarLiberadosAsync()
        {
            return await _context.BonusMotoristas.CountAsync(b => b.Liberado);
        }

        public async Task<IEnumerable<BonusMotorista>> ListarAsync()
        {
            return await _context.BonusMotoristas.OrderBy(b => b.DataConcedido).ToListAsync();
        }

        public async Task<BonusMotorista?> ObterPorMotoristaIdAsync(Guid motoristaId)
        {
            return await _context.BonusMotoristas.FirstOrDefaultAsync(b => b.MotoristaId == motoristaId);
        }

        public async Task AdicionarAsync(BonusMotorista bonus)
        {
            await _context.BonusMotoristas.AddAsync(bonus);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(BonusMotorista bonus)
        {
            _context.BonusMotoristas.Update(bonus);
            await _context.SaveChangesAsync();
        }
    }
}
