using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class SolicitacaoSaqueConfiguration : IEntityTypeConfiguration<SolicitacaoSaque>
    {
        public void Configure(EntityTypeBuilder<SolicitacaoSaque> builder)
        {
            builder.ToTable("SolicitacoesSaque");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.MotoristaId)
                .IsRequired();

            builder.HasIndex(s => new { s.MotoristaId, s.DataSolicitacao });
            builder.HasIndex(s => s.Status);

            builder.Property(s => s.Valor)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(s => s.Tipo)
                .IsRequired();

            builder.Property(s => s.ChavePix).HasMaxLength(200);
            builder.Property(s => s.Banco).HasMaxLength(120);
            builder.Property(s => s.Agencia).HasMaxLength(20);
            builder.Property(s => s.Conta).HasMaxLength(30);
            builder.Property(s => s.TipoConta).HasMaxLength(20);
            builder.Property(s => s.MotivoRejeicao).HasMaxLength(300);

            builder.Property(s => s.Status)
                .IsRequired();

            builder.Property(s => s.DataSolicitacao)
                .IsRequired();

            builder.HasOne<Motorista>()
                .WithMany()
                .HasForeignKey(s => s.MotoristaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
