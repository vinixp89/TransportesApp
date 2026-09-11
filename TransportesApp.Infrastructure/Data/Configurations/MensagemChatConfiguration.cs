using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class MensagemChatConfiguration : IEntityTypeConfiguration<MensagemChat>
    {
        public void Configure(EntityTypeBuilder<MensagemChat> builder)
        {
            builder.ToTable("MensagensChat");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.RemetenteTipo)
                .IsRequired();

            builder.Property(m => m.Texto)
                .IsRequired()
                .HasMaxLength(MensagemChat.TamanhoMaximoTexto);

            builder.Property(m => m.DataEnvio)
                .IsRequired();

            // Listagem sempre por corrida, ordenada por data — o índice cobre exatamente essa consulta
            // (ver MensagemChatRepository.ListarPorCorridaAsync, incluindo o filtro incremental "desde").
            builder.HasIndex(m => new { m.CorridaId, m.DataEnvio });

            builder.HasOne<Corrida>()
                .WithMany()
                .HasForeignKey(m => m.CorridaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
