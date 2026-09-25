using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PromocaoOutubroEAvisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromocoesLancamento_ClienteId",
                table: "PromocoesLancamento");

            migrationBuilder.AddColumn<int>(
                name: "Campanha",
                table: "PromocoesLancamento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Avisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CorFundoHex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    TextoBotao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TelaDestino = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avisos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromocoesLancamento_ClienteId_Campanha",
                table: "PromocoesLancamento",
                columns: new[] { "ClienteId", "Campanha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Avisos");

            migrationBuilder.DropIndex(
                name: "IX_PromocoesLancamento_ClienteId_Campanha",
                table: "PromocoesLancamento");

            migrationBuilder.DropColumn(
                name: "Campanha",
                table: "PromocoesLancamento");

            migrationBuilder.CreateIndex(
                name: "IX_PromocoesLancamento_ClienteId",
                table: "PromocoesLancamento",
                column: "ClienteId",
                unique: true);
        }
    }
}
