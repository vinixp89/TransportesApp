using Microsoft.EntityFrameworkCore;
using TransportesApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransportesApp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        
        {
        
        
        
        
        }


        public DbSet<Corrida> Corridas => Set<Corrida>();
        public DbSet<Motorista> Motoristas => Set<Motorista>();
        public DbSet<Cliente> Clientes => Set<Cliente>();


       protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

    }
}
