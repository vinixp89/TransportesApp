# Build feito de dentro do repo inteiro (contexto = raiz do repositório, ver docker-compose.yml)
# pra conseguir copiar os 4 projetos (Api, Application, Domain, Infrastructure) que se referenciam.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia só os .csproj primeiro e restaura — assim o Docker reaproveita essa camada em cache
# enquanto só o código (não as dependências) mudar entre builds.
COPY TransportesApp.Domain/TransportesApp.Domain.csproj TransportesApp.Domain/
COPY TransportesApp.Application/TransportesApp.Application.csproj TransportesApp.Application/
COPY TransportesApp.Infrastructure/TransportesApp.Infrastructure.csproj TransportesApp.Infrastructure/
COPY TransportesApp.Api/TransportesApp.Api.csproj TransportesApp.Api/
RUN dotnet restore TransportesApp.Api/TransportesApp.Api.csproj

COPY . .
RUN dotnet publish TransportesApp.Api/TransportesApp.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# A imagem base não vem com libgssapi_krb5 — sem ela, o .NET não consegue abrir certas conexões
# HTTPS de saída (ex: chamadas pro Google Maps/Mercado Pago), mesmo a API subindo normalmente.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

# Plataformas tipo Railway injetam a porta a usar via variável de ambiente PORT (muda a cada
# deploy); no docker-compose/EC2 essa variável não existe, então cai no 8080 fixo de sempre.
ENTRYPOINT ["sh", "-c", "exec dotnet TransportesApp.Api.dll --urls http://+:${PORT:-8080}"]
