using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CategoriaExecutivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnoVeiculo",
                table: "Motoristas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Categoria",
                table: "Corridas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AssinaturasMotoristaExecutivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotoristaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataCancelamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssinaturasMotoristaExecutivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssinaturasMotoristaExecutivo_Motoristas_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Motoristas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasMotoristaExecutivo_MotoristaId",
                table: "AssinaturasMotoristaExecutivo",
                column: "MotoristaId",
                unique: true,
                filter: "\"Status\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssinaturasMotoristaExecutivo");

            migrationBuilder.DropColumn(
                name: "AnoVeiculo",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Corridas");
        }
    }
}
