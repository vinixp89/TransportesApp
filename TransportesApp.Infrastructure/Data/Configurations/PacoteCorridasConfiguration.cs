using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class PacoteCorridasConfiguration : IEntityTypeConfiguration<PacoteCorridas>
    {
        public void Configure(EntityTypeBuilder<PacoteCorridas> builder)
        {
            builder.ToTable("PacotesCorridas");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.ClienteId)
                .IsRequired();

            builder.Property(p => p.Faixa)
                .IsRequired();

            builder.Property(p => p.QuantidadeTotal)
                .IsRequired();

            builder.Property(p => p.QuantidadeUsada)
                .IsRequired();

            builder.Property(p => p.PrecoPago)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(p => p.DataCompra)
                .IsRequired();

            builder.Property(p => p.EhPromocional)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
