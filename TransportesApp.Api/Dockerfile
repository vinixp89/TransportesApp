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
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TransportesApp.Api.dll"]
