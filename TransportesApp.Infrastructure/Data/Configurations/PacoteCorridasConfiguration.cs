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

            // Default true protege os pacotes já comprados antes dessa coluna existir — só a
            // partir de agora que novas compras nascem com false até o pagamento confirmar.
            builder.Property(p => p.Pago)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
