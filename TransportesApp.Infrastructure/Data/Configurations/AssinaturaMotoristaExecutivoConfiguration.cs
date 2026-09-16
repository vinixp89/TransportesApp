using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class AssinaturaMotoristaExecutivoConfiguration : IEntityTypeConfiguration<AssinaturaMotoristaExecutivo>
    {
        public void Configure(EntityTypeBuilder<AssinaturaMotoristaExecutivo> builder)
        {
            builder.ToTable("AssinaturasMotoristaExecutivo");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.MotoristaId)
                .IsRequired();

            builder.Property(a => a.DataInicio)
                .IsRequired();

            builder.Property(a => a.Status)
                .IsRequired();

            builder.Property(a => a.PreapprovalId)
                .HasMaxLength(100);

            builder.Property(a => a.CheckoutUrl)
                .HasMaxLength(500);

            builder.Property(a => a.EmailPagador)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(a => a.MotivoNegacao)
                .HasMaxLength(500);

            // Um motorista só pode ter UMA assinatura Executivo "em aberto" por vez — mesmo índice parcial
            // do AssinaturaPlano (0 = PendentePagamento, 1 = Ativa), agora incluindo também 4 =
            // AguardandoAprovacao (ver StatusAssinatura).
            builder.HasIndex(a => a.MotoristaId)
                .IsUnique()
                .HasFilter("\"Status\" IN (0, 1, 4)");

            builder.HasOne<Motorista>()
                .WithMany()
                .HasForeignKey(a => a.MotoristaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
