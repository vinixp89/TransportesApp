using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPacoteCorridas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PacotesCorridas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Faixa = table.Column<int>(type: "integer", nullable: false),
                    QuantidadeTotal = table.Column<int>(type: "integer", nullable: false),
                    QuantidadeUsada = table.Column<int>(type: "integer", nullable: false),
                    PrecoPago = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DataCompra = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacotesCorridas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PacotesCorridas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Corridas_PacoteCorridasId",
                table: "Corridas",
                column: "PacoteCorridasId");

            migrationBuilder.CreateIndex(
                name: "IX_PacotesCorridas_ClienteId",
                table: "PacotesCorridas",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Corridas_PacotesCorridas_PacoteCorridasId",
                table: "Corridas",
                column: "PacoteCorridasId",
                principalTable: "PacotesCorridas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Corridas_PacotesCorridas_PacoteCorridasId",
                table: "Corridas");

            migrationBuilder.DropTable(
                name: "PacotesCorridas");

            migrationBuilder.DropIndex(
                name: "IX_Corridas_PacoteCorridasId",
                table: "Corridas");
        }
    }
}
