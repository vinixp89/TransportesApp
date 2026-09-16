using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TelefoneTermosSelfieEChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataAceiteTermos",
                table: "Motoristas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Motoristas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TelefoneVerificado",
                table: "Motoristas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TermosAceitos",
                table: "Motoristas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAceiteTermos",
                table: "Clientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoSelfieUrl",
                table: "Clientes",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TelefoneVerificado",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TermosAceitos",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MensagensChat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorridaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RemetenteTipo = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensChat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensChat_Corridas_CorridaId",
                        column: x => x.CorridaId,
                        principalTable: "Corridas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensChat_CorridaId_DataEnvio",
                table: "MensagensChat",
                columns: new[] { "CorridaId", "DataEnvio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensagensChat");

            migrationBuilder.DropColumn(
                name: "DataAceiteTermos",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "TelefoneVerificado",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "TermosAceitos",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "DataAceiteTermos",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "FotoSelfieUrl",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TelefoneVerificado",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TermosAceitos",
                table: "Clientes");
        }
    }
}
