using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class AvaliacaoConfiguration : IEntityTypeConfiguration<Avaliacao>
    {
        public void Configure(EntityTypeBuilder<Avaliacao> builder)
        {
            builder.ToTable("Avaliacoes");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.AutorTipo)
                .IsRequired();

            builder.Property(a => a.AvaliadoId)
                .IsRequired();

            builder.Property(a => a.Nota)
                .IsRequired();

            builder.Property(a => a.Comentario)
                .HasMaxLength(Avaliacao.TamanhoMaximoComentario);

            builder.Property(a => a.DataAvaliacao)
                .IsRequired();

            // No máximo uma avaliação por corrida em cada direção (Cliente→Motorista, Motorista→Cliente)
            // — ver AvaliacaoService.AvaliarAsync, que também confere isso antes de inserir.
            builder.HasIndex(a => new { a.CorridaId, a.AutorTipo })
                .IsUnique();

            // Usado pra recalcular a média de quem foi avaliado (ver
            // AvaliacaoRepository.ObterMediaAsync) — consulta sempre por AvaliadoId.
            builder.HasIndex(a => a.AvaliadoId);

            builder.HasOne<Corrida>()
                .WithMany()
                .HasForeignKey(a => a.CorridaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
