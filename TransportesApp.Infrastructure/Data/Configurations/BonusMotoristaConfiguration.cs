using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class BonusMotoristaConfiguration : IEntityTypeConfiguration<BonusMotorista>
    {
        public void Configure(EntityTypeBuilder<BonusMotorista> builder)
        {
            builder.ToTable("BonusMotoristas");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.MotoristaId).IsRequired();

            builder.Property(b => b.Valor)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(b => b.DataConcedido).IsRequired();

            builder.HasIndex(b => b.MotoristaId).IsUnique();

            builder.HasOne<Motorista>()
                .WithMany()
                .HasForeignKey(b => b.MotoristaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
