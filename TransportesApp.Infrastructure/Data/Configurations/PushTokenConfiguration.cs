using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class PushTokenConfiguration : IEntityTypeConfiguration<PushToken>
    {
        public void Configure(EntityTypeBuilder<PushToken> builder)
        {
            builder.ToTable("PushTokens");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.UsuarioId)
                .IsRequired();

            builder.Property(p => p.Papel)
                .IsRequired();

            builder.Property(p => p.Token)
                .IsRequired()
                .HasMaxLength(200);

            // Um mesmo token físico nunca fica duplicado — se reaparecer (reinstalação, troca de
            // conta no mesmo aparelho), é atualizado em vez de criar linha nova.
            builder.HasIndex(p => p.Token)
                .IsUnique();

            builder.Property(p => p.DataAtualizacao)
                .IsRequired();
        }
    }
}
