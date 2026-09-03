using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class CarteiraMotoristaConfiguration : IEntityTypeConfiguration<CarteiraMotorista>
    {
        public void Configure(EntityTypeBuilder<CarteiraMotorista> builder)
        {
            builder.ToTable("CarteirasMotorista");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.MotoristaId)
                .IsRequired();

            // Um motorista só pode ter uma carteira.
            builder.HasIndex(c => c.MotoristaId)
                .IsUnique();

            builder.Property(c => c.Saldo)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(c => c.DataCriacao)
                .IsRequired();

            builder.HasOne<Motorista>()
                .WithMany()
                .HasForeignKey(c => c.MotoristaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
