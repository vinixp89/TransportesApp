using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class MensagemSuporteConfiguration : IEntityTypeConfiguration<MensagemSuporte>
    {
        public void Configure(EntityTypeBuilder<MensagemSuporte> builder)
        {
            builder.ToTable("MensagensSuporte");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.TipoUsuario)
                .IsRequired();

            builder.Property(m => m.EnviadaPeloAdmin)
                .IsRequired();

            builder.Property(m => m.Texto)
                .IsRequired()
                .HasMaxLength(MensagemSuporte.TamanhoMaximoTexto);

            builder.Property(m => m.DataEnvio)
                .IsRequired();

            // Listagem sempre por usuário, ordenada por data — cobre tanto o polling incremental
            // quanto a busca da última mensagem de cada conversa (ver MensagemSuporteRepository).
            builder.HasIndex(m => new { m.UsuarioId, m.DataEnvio });

            // Sem FK pra Cliente/Motorista de propósito: UsuarioId é o Id da conta (Identity), não o
            // Id da entidade Cliente/Motorista — mesma convenção usada em Carteira, Notificacao etc.
        }
    }
}
