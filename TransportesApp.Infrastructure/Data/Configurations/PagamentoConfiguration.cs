using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportesApp.Domain.Entities;

namespace TransportesApp.Infrastructure.Data.Configurations
{
    public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
    {
        public void Configure(EntityTypeBuilder<Pagamento> builder)
        {
            builder.ToTable("Pagamentos");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.ClienteId)
                .IsRequired();

            builder.Property(p => p.TipoReferencia)
                .IsRequired();

            builder.Property(p => p.ReferenciaId)
                .IsRequired();

            builder.Property(p => p.Valor)
                .IsRequired()
                .HasColumnType("numeric(10,2)");

            builder.Property(p => p.Descricao)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.PreferenceId)
                .HasMaxLength(100);

            builder.Property(p => p.PagamentoGatewayId)
                .HasMaxLength(100);

            builder.Property(p => p.DataCriacao)
                .IsRequired();

            // Não é FK de verdade pra nenhuma tabela (ReferenciaId aponta pra AssinaturaPlano, ou no
            // futuro PacotesCorridas/Corridas, dependendo de TipoReferencia) — por isso fica sem
            // HasOne/HasForeignKey, só um índice pra achar rápido "os pagamentos dessa referência".
            builder.HasIndex(p => new { p.TipoReferencia, p.ReferenciaId });

            // Busca frequente no webhook/sincronização: achar o Pagamento pela ExternalReference que a
            // gente manda pro gateway, que é sempre o próprio Pagamento.Id — então essa busca real é por
            // Id (chave primária, já indexada). Esse índice aqui é só pra consulta por status do gateway.
            builder.HasIndex(p => p.PagamentoGatewayId);
        }
    }
}
