using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
    {
        public void Configure(EntityTypeBuilder<Notificacao> builder)
        {
            builder.ToTable("Notificacoes");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.ClienteId)
                .IsRequired();

            builder.Property(n => n.Titulo)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(n => n.Mensagem)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(n => n.Tipo)
                .IsRequired();

            builder.Property(n => n.Lida)
                .IsRequired();

            builder.Property(n => n.DataCriacao)
                .IsRequired();

            // Caixa de entrada lista por cliente, mais recentes primeiro — índice cobre os dois.
            builder.HasIndex(n => new { n.ClienteId, n.DataCriacao });

            builder.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(n => n.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
