
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MercadoPago.Config;
using TransportesApp.Application.Services;
using TransportesApp.Domain.Entities;
using TransportesApp.Domain.Interfaces;
using TransportesApp.Infrastructure.Data;
using TransportesApp.Infrastructure.Email;
using TransportesApp.Infrastructure.Maps;
using TransportesApp.Infrastructure.Pagamentos;
using TransportesApp.Infrastructure.Repositories;
using Microsoft.OpenApi;

namespace TransportesApp.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddMemoryCache();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
            builder.Services.AddScoped<ICorridaRepository, CorridaRepository>();
            builder.Services.AddScoped<IMotoristaRepository, MotoristaRepository>();
            builder.Services.AddScoped<IPacoteCorridasRepository, PacoteCorridasRepository>();
            builder.Services.AddScoped<ICarteiraRepository, CarteiraRepository>();
            builder.Services.AddScoped<ITransacaoCarteiraRepository, TransacaoCarteiraRepository>();
            builder.Services.AddScoped<IAssinaturaPlanoRepository, AssinaturaPlanoRepository>();
            builder.Services.AddScoped<IPagamentoRepository, PagamentoRepository>();
            builder.Services.AddScoped<IGatewayPagamento, MercadoPagoGateway>();
            builder.Services.AddScoped<ClienteService>();
            builder.Services.AddScoped<MotoristaService>();
            builder.Services.AddScoped<CorridaService>();
            builder.Services.AddScoped<PacoteCorridasService>();
            builder.Services.AddScoped<CarteiraService>();
            builder.Services.AddScoped<PagamentoService>();
            builder.Services.AddScoped<PlanoService>();
            builder.Services.AddScoped<EnderecoAutocompleteService>();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddHttpClient<IMapsService, GoogleMapsService>();

            // Access Token do Mercado Pago é lido uma vez aqui no startup (via User Secrets:
            // "MercadoPago:AccessToken") e guardado como estado estático do SDK — ver MercadoPagoGateway.
            // Fica vazio de propósito em appsettings.json; sem ele configurado, qualquer tentativa de
            // pagamento falha com uma mensagem clara em vez de silenciosamente usar uma conta errada.
            var mercadoPagoAccessToken = builder.Configuration["MercadoPago:AccessToken"];
            if (!string.IsNullOrWhiteSpace(mercadoPagoAccessToken))
                MercadoPagoConfig.AccessToken = mercadoPagoAccessToken;

            // Libera o front-end a chamar a API — sem isso o navegador bloqueia as requisições por
            // CORS mesmo a API respondendo normalmente. Em produção a origem real (domínio do
            // front-end) vem de "Cors:AllowedOrigins" (configurável via variável de ambiente
            // Cors__AllowedOrigins__0 no docker-compose); localhost:5173 (Vite) sempre liberado
            // pra não quebrar o desenvolvimento local.
            var origensLiberadas = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            var origensCors = origensLiberadas.Concat(["http://localhost:5173", "https://localhost:5173"]).Distinct().ToArray();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendDev", policy =>
                {
                    policy.WithOrigins(origensCors)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Em produção a API fica atrás do Caddy (reverse proxy que faz TLS), então toda
            // requisição chega no container por HTTP puro — sem isso, o ASP.NET Core não sabe que
            // o pedido original era HTTPS e o UseHttpsRedirection() abaixo entraria num loop de
            // redirecionamento.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Digite: Bearer {seu token}"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            builder.Services.AddIdentity<Usuario, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
                };
            });

            var app = builder.Build();

            app.UseForwardedHeaders();

            using (var scope = app.Services.CreateScope())
            {
                // Aplica migrations pendentes automaticamente no start — em produção (container
                // Docker) não tem Visual Studio/Package Manager Console pra rodar Update-Database
                // manualmente. Idempotente: migrations já aplicadas são ignoradas.
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                string[] roles = { "Cliente", "Motorista", "Admin" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }

                // Cria a conta de Admin automaticamente no start, se as credenciais estiverem configuradas
                // (via User Secrets: "Admin:Email" e "Admin:Password") e a conta ainda não existir.
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var adminEmail = configuration["Admin:Email"];
                var adminSenha = configuration["Admin:Password"];

                if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminSenha))
                {
                    var adminExistente = await userManager.FindByEmailAsync(adminEmail);

                    if (adminExistente is null)
                    {
                        var admin = new Usuario { UserName = adminEmail, Email = adminEmail };
                        var resultadoAdmin = await userManager.CreateAsync(admin, adminSenha);

                        if (resultadoAdmin.Succeeded)
                            await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // Redirecionar pra HTTPS só faz sentido fora do Development — em dev a API roda
                // em HTTP puro de propósito (testes locais via rede Wi-Fi/túnel com o app mobile),
                // e o React Native não segue redirecionamento 307 corretamente em requisições
                // POST, o que quebrava o login vindo do app.
                app.UseHttpsRedirection();
            }

            app.UseCors("FrontendDev");

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
