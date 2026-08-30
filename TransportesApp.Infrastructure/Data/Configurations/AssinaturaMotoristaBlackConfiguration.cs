using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class AssinaturaMotoristaBlackConfiguration : IEntityTypeConfiguration<AssinaturaMotoristaBlack>
    {
        public void Configure(EntityTypeBuilder<AssinaturaMotoristaBlack> builder)
        {
            builder.ToTable("AssinaturasMotoristaBlack");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.MotoristaId)
                .IsRequired();

            builder.Property(a => a.DataInicio)
                .IsRequired();

            builder.Property(a => a.Status)
                .IsRequired();

            // Um motorista só pode ter UMA assinatura Black "em aberto" por vez — mesmo índice parcial
            // do AssinaturaPlano (0 = PendentePagamento, 1 = Ativa).
            builder.HasIndex(a => a.MotoristaId)
                .IsUnique()
                .HasFilter("\"Status\" IN (0, 1)");

            builder.HasOne<Motorista>()
                .WithMany()
                .HasForeignKey(a => a.MotoristaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
