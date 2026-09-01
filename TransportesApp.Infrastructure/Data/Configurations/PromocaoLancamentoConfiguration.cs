using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class PromocaoLancamentoConfiguration : IEntityTypeConfiguration<PromocaoLancamento>
    {
        public void Configure(EntityTypeBuilder<PromocaoLancamento> builder)
        {
            builder.ToTable("PromocoesLancamento");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.ClienteId)
                .IsRequired();

            builder.Property(p => p.Faixa)
                .IsRequired();

            builder.Property(p => p.DataConcedida)
                .IsRequired();

            // Um cliente só pode receber a promoção uma vez.
            builder.HasIndex(p => p.ClienteId)
                .IsUnique();

            builder.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
