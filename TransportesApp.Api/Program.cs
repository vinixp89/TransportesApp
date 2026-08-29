
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
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
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Connection string do Postgres fica vazia de propósito em appsettings.json (mesmo padrão do
            // MercadoPago/GoogleMaps/LocationIq abaixo) — configure a sua localmente via User Secrets:
            // dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=transportesapp_db;Username=postgres;Password=SUA_SENHA"
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

            // Libera o front-end (React rodando em localhost:5173, porta padrão do Vite) a chamar a API.
            // Sem isso o navegador bloqueia as requisições por CORS mesmo a API respondendo normalmente.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendDev", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Limita tentativas de login por IP pra dificultar força bruta/credential stuffing contra
            // /api/Auth/login — 5 tentativas por minuto, sem fila (excedente recebe 429 direto).
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter("login", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 5;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueLimit = 0;
                });
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

                // Bloqueia a conta após tentativas de senha erradas seguidas — junto com o rate limiter
                // do login, isso é o que efetivamente impede força bruta (o CheckPasswordSignInAsync do
                // AuthController é quem incrementa/zera esse contador).
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
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

                // Revogação de token: o SecurityStamp do usuário (claim "security_stamp", ver
                // AuthController.GerarTokenAsync) é comparado com o valor atual no banco a cada request.
                // O Identity troca o SecurityStamp sozinho ao trocar senha — então um token vazado ou de
                // uma sessão antiga vira inválido imediatamente, em vez de continuar valendo até expirar.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();

                        var userId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                        var tokenStamp = context.Principal?.FindFirstValue("security_stamp");

                        var usuario = userId is not null ? await userManager.FindByIdAsync(userId) : null;

                        if (usuario is null || tokenStamp is null ||
                            !string.Equals(await userManager.GetSecurityStampAsync(usuario), tokenStamp, StringComparison.Ordinal))
                        {
                            context.Fail("Sessão inválida ou expirada — faça login novamente.");
                        }
                    }
                };
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
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
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("FrontendDev");

            app.UseRateLimiter();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            await app.RunAsync();
        }
    }
}
