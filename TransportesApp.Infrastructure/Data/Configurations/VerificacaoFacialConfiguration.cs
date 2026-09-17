using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class VerificacaoFacialConfiguration : IEntityTypeConfiguration<VerificacaoFacial>
    {
        public void Configure(EntityTypeBuilder<VerificacaoFacial> builder)
        {
            builder.ToTable("VerificacoesFaciais");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Momento)
                .IsRequired();

            builder.Property(v => v.FotoUrl)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(v => v.Processada)
                .IsRequired();

            builder.Property(v => v.ErroProcessamento)
                .HasMaxLength(300);

            builder.Property(v => v.DataHora)
                .IsRequired();

            builder.HasIndex(v => v.CorridaId);
            builder.HasIndex(v => v.MotoristaId);

            builder.HasOne<Corrida>()
                .WithMany()
                .HasForeignKey(v => v.CorridaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
