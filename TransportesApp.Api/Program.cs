
using Microsoft.EntityFrameworkCore;
using TransportesApp.Application.Services;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;
using TransportesApp.Infrastructure.Repositories;

namespace TransportesApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
            builder.Services.AddScoped<ICorridaRepository, CorridaRepository>();
            builder.Services.AddScoped<IMotoristaRepository, MotoristaRepository>();
            builder.Services.AddScoped<ClienteService>();
            builder.Services.AddScoped<MotoristaService>();
            builder.Services.AddScoped<CorridaService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
