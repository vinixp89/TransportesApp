using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class MotoristaConfiguration : IEntityTypeConfiguration<Motorista>
    {
        public void Configure(EntityTypeBuilder<Motorista> builder)
        {
            builder.ToTable("Motoristas");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.CNH)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(m => m.Cpf)
                .IsRequired()
                .HasMaxLength(11);

            builder.HasIndex(m => m.Cpf)
                .IsUnique();

            builder.Property(m => m.PlacaVeiculo)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(m => m.ModeloVeiculo)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.DataCadastro)
                .IsRequired();

            builder.Property(m => m.FotoSelfieUrl)
                .HasMaxLength(300);

            builder.Property(m => m.FotoVeiculoUrl)
                .HasMaxLength(300);

            builder.Property(m => m.FotoPlacaUrl)
                .HasMaxLength(300);

            builder.OwnsOne(m => m.Endereco, endereco =>
            {
                endereco.Property(e => e.Logradouro)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("Endereco_Logradouro");

                endereco.Property(e => e.Numero)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("Endereco_Numero");

                endereco.Property(e => e.Bairro)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("Endereco_Bairro");

                endereco.Property(e => e.Cidade)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("Endereco_Cidade");

                endereco.Property(e => e.Estado)
                    .IsRequired()
                    .HasMaxLength(2)
                    .HasColumnName("Endereco_Estado");

                endereco.Property(e => e.Complemento)
                    .HasMaxLength(100)
                    .HasColumnName("Endereco_Complemento");

                endereco.Property(e => e.Latitude)
                    .HasColumnName("Endereco_Latitude");

                endereco.Property(e => e.Longitude)
                    .HasColumnName("Endereco_Longitude");
            });
        }
    }
}
