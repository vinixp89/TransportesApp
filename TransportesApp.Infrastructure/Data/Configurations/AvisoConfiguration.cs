using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class AvisoConfiguration : IEntityTypeConfiguration<Aviso>
    {
        public void Configure(EntityTypeBuilder<Aviso> builder)
        {
            builder.ToTable("Avisos");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Titulo)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(a => a.Texto)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.CorFundoHex)
                .IsRequired()
                .HasMaxLength(9);

            builder.Property(a => a.TextoBotao)
                .HasMaxLength(40);

            builder.Property(a => a.TelaDestino)
                .HasMaxLength(60);

            builder.Property(a => a.DataInicio)
                .IsRequired();

            builder.Property(a => a.DataFim)
                .IsRequired();

            builder.Property(a => a.Ativo)
                .IsRequired();
        }
    }
}
