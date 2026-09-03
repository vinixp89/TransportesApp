using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TransportesApp.Domain.Entities;


namespace TransportesApp.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        
        {
        
        
        
        
        }


        public DbSet<Corrida> Corridas => Set<Corrida>();
        public DbSet<Motorista> Motoristas => Set<Motorista>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<PacoteCorridas> PacotesCorridas => Set<PacoteCorridas>();
        public DbSet<Carteira> Carteiras => Set<Carteira>();
        public DbSet<TransacaoCarteira> TransacoesCarteira => Set<TransacaoCarteira>();
        public DbSet<AssinaturaPlano> AssinaturasPlano => Set<AssinaturaPlano>();
        public DbSet<AssinaturaMotoristaExecutivo> AssinaturasMotoristaExecutivo => Set<AssinaturaMotoristaExecutivo>();
        public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
        public DbSet<PromocaoLancamento> PromocoesLancamento => Set<PromocaoLancamento>();
        public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
        public DbSet<CarteiraMotorista> CarteirasMotorista => Set<CarteiraMotorista>();
        public DbSet<TransacaoCarteiraMotorista> TransacoesCarteiraMotorista => Set<TransacaoCarteiraMotorista>();
        public DbSet<SolicitacaoSaque> SolicitacoesSaque => Set<SolicitacaoSaque>();


       protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

    }
}
