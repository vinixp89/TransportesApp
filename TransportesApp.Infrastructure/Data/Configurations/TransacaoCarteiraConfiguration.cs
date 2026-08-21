using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class TransacaoCarteiraConfiguration : IEntityTypeConfiguration<TransacaoCarteira>
    {
        public void Configure(EntityTypeBuilder<TransacaoCarteira> builder)
        {
            builder.ToTable("TransacoesCarteira");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.CarteiraId)
                .IsRequired();

            builder.HasIndex(t => t.CarteiraId);

            builder.Property(t => t.Tipo)
                .IsRequired();

            builder.Property(t => t.Valor)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(t => t.Data)
                .IsRequired();

            builder.Property(t => t.Descricao)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne<Carteira>()
                .WithMany()
                .HasForeignKey(t => t.CarteiraId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
