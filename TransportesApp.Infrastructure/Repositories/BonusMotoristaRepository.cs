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

        public async Task<bool> MotoristaJaRecebeuAsync(Guid motoristaId)
        {
            return await _context.BonusMotoristas.AnyAsync(b => b.MotoristaId == motoristaId);
        }

        public async Task AdicionarAsync(BonusMotorista bonus)
        {
            await _context.BonusMotoristas.AddAsync(bonus);
            await _context.SaveChangesAsync();
        }
    }
}
