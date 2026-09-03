using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class TransacaoCarteiraMotoristaConfiguration : IEntityTypeConfiguration<TransacaoCarteiraMotorista>
    {
        public void Configure(EntityTypeBuilder<TransacaoCarteiraMotorista> builder)
        {
            builder.ToTable("TransacoesCarteiraMotorista");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.CarteiraMotoristaId)
                .IsRequired();

            builder.HasIndex(t => t.CarteiraMotoristaId);

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

            builder.HasOne<CarteiraMotorista>()
                .WithMany()
                .HasForeignKey(t => t.CarteiraMotoristaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
