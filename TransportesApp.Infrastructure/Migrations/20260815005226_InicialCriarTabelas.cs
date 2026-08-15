using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InicialCriarTabelas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Endereço_Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Endereço_Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Endereço_Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endereço_Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endereço_Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Endereço_complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Endereço_Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Endereço_Longitude = table.Column<double>(type: "double precision", nullable: false),
                    AvaliacaoMedia = table.Column<double>(type: "double precision", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Corridas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MotoristaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origem_Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Origem_Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Origem_Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Origem_Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Origem_Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Origem_Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Origem_Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Origem_Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Destino_Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Destino_Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Destino_Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Destino_Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Destino_Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Destino_Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Destino_Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Destino_Longitude = table.Column<double>(type: "double precision", nullable: false),
                    DistanciaEstimadaKm = table.Column<double>(type: "double precision", nullable: false),
                    DistanciaRealKm = table.Column<double>(type: "double precision", nullable: true),
                    FaixaContratada = table.Column<int>(type: "integer", nullable: false),
                    TipoConsumo = table.Column<int>(type: "integer", nullable: false),
                    PacoteCorridasId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValorExcedente = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFinalizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corridas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Motoristas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CNH = table.Column<string>(type: "text", nullable: false),
                    PlacaVeiculo = table.Column<string>(type: "text", nullable: false),
                    ModeloVeiculo = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LatitudeAtual = table.Column<double>(type: "double precision", nullable: true),
                    LongitudeAtual = table.Column<double>(type: "double precision", nullable: true),
                    AvaliacaoMedia = table.Column<double>(type: "double precision", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motoristas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Corridas");

            migrationBuilder.DropTable(
                name: "Motoristas");
        }
    }
}
