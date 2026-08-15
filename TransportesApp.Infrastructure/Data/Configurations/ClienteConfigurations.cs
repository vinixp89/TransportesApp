using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TransportesApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.ValueObjects;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public  class ClienteConfigurations: IEntityTypeConfiguration<Cliente>
    { public void Configure(EntityTypeBuilder<Cliente> builder) 
        {
            builder.ToTable("Clientes");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Telefone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c=>c.AvaliacaoMedia);

            builder.Property(c => c.DataCadastro)
                    .IsRequired();

            builder.OwnsOne(c => c.Endereco, endereco =>

            {
                endereco.Property(e => e.Logradouro)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("Endereço_Logradouro");

                endereco.Property(e => e.Numero)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("Endereço_Numero");

                endereco.Property(c => c.Bairro)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Endereço_Bairro");

                endereco.Property(c => c.Cidade)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Endereço_Cidade");

                endereco.Property(c => c.Estado)
                .IsRequired()
                .HasMaxLength(2)
                .HasColumnName("Endereço_Estado");

                endereco.Property(c => c.Complemento)
                
                .HasMaxLength(100)
                .HasColumnName("Endereço_complemento");

                endereco.Property(c => c.Latitude)
                .HasColumnName("Endereço_Latitude");

                endereco.Property(c => c.Longitude)
                .HasColumnName("Endereço_Longitude");




            });
        
        }
    }
}
