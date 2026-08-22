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

            // Controle do benefício de corrida grátis mensal (ver AssinaturaPlano) — AnoMesBeneficio fica
            // null até a primeira corrida paga/grátis da assinatura, os dois flags nascem false por padrão.
            builder.Property(a => a.AnoMesBeneficio);

            builder.Property(a => a.CorridaPagaNoMes)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(a => a.BeneficioUsadoNoMes)
                .IsRequired()
                .HasDefaultValue(false);

            // Um cliente só pode ter UMA assinatura ativa por vez — índice parcial (só considera
            // linhas com Ativa = true), então o histórico de assinaturas canceladas não conta pra
            // essa restrição.
            builder.HasIndex(a => a.ClienteId)
                .IsUnique()
                .HasFilter("\"Ativa\" = true");

            builder.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(a => a.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
