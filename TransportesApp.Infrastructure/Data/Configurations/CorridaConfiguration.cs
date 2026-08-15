using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class CorridaConfiguration : IEntityTypeConfiguration<Corrida>
    {
        public void Configure(EntityTypeBuilder<Corrida> builder)
        {
            builder.ToTable("Corridas");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.ClienteId)
                .IsRequired();

            builder.Property(c => c.DistanciaEstimadaKm)
                .IsRequired();

            builder.Property(c => c.FaixaContratada)
                .IsRequired();

            builder.Property(c => c.TipoConsumo)
                .IsRequired();

            builder.Property(c => c.Status)
                .IsRequired();

            builder.Property(c => c.DataSolicitacao)
                .IsRequired();

            // Origem — owned type, prefixo "Origem_" nas colunas
            builder.OwnsOne(c => c.Origem, origem =>
            {
                origem.Property(e => e.Logradouro).IsRequired().HasMaxLength(200).HasColumnName("Origem_Logradouro");
                origem.Property(e => e.Numero).IsRequired().HasMaxLength(20).HasColumnName("Origem_Numero");
                origem.Property(e => e.Bairro).HasMaxLength(100).HasColumnName("Origem_Bairro");
                origem.Property(e => e.Cidade).IsRequired().HasMaxLength(100).HasColumnName("Origem_Cidade");
                origem.Property(e => e.Estado).HasMaxLength(2).HasColumnName("Origem_Estado");
                origem.Property(e => e.Complemento).HasMaxLength(100).HasColumnName("Origem_Complemento");
                origem.Property(e => e.Latitude).HasColumnName("Origem_Latitude");
                origem.Property(e => e.Longitude).HasColumnName("Origem_Longitude");
            });

            // Destino — owned type, prefixo "Destino_" nas colunas
            builder.OwnsOne(c => c.Destino, destino =>
            {
                destino.Property(e => e.Logradouro).IsRequired().HasMaxLength(200).HasColumnName("Destino_Logradouro");
                destino.Property(e => e.Numero).IsRequired().HasMaxLength(20).HasColumnName("Destino_Numero");
                destino.Property(e => e.Bairro).HasMaxLength(100).HasColumnName("Destino_Bairro");
                destino.Property(e => e.Cidade).IsRequired().HasMaxLength(100).HasColumnName("Destino_Cidade");
                destino.Property(e => e.Estado).HasMaxLength(2).HasColumnName("Destino_Estado");
                destino.Property(e => e.Complemento).HasMaxLength(100).HasColumnName("Destino_Complemento");
                destino.Property(e => e.Latitude).HasColumnName("Destino_Latitude");
                destino.Property(e => e.Longitude).HasColumnName("Destino_Longitude");
            });
        }
    }
}