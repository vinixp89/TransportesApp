using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class AssinaturaPlanoConfiguration : IEntityTypeConfiguration<AssinaturaPlano>
    {
        public void Configure(EntityTypeBuilder<AssinaturaPlano> builder)
        {
            builder.ToTable("AssinaturasPlano");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.ClienteId)
                .IsRequired();

            builder.Property(a => a.Tipo)
                .IsRequired();

            builder.Property(a => a.DataInicio)
                .IsRequired();

            builder.Property(a => a.Status)
                .IsRequired();

            // Controle do benefício de corrida grátis mensal (ver AssinaturaPlano) — AnoMesBeneficio fica
            // null até a primeira corrida paga/grátis da assinatura, os dois flags nascem false por padrão.
            builder.Property(a => a.AnoMesBeneficio);

            builder.Property(a => a.CorridaPagaNoMes)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(a => a.BeneficioUsadoNoMes)
                .IsRequired()
                .HasDefaultValue(false);

            // Um cliente só pode ter UMA assinatura "em aberto" por vez — índice parcial que considera
            // só PendentePagamento (0) e Ativa (1), os dois status não-finais de StatusAssinatura. Assim
            // o histórico de assinaturas canceladas/com pagamento recusado não conta pra essa restrição,
            // mas ainda impede o cliente de ter duas tentativas de assinatura em aberto ao mesmo tempo.
            builder.HasIndex(a => a.ClienteId)
                .IsUnique()
                .HasFilter("\"Status\" IN (0, 1)");

            builder.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(a => a.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
