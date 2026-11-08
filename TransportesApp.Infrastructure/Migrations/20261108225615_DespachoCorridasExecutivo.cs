using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DespachoCorridasExecutivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvisosMotoristasEnviados",
                table: "Corridas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataDisponivel",
                table: "Corridas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentualMotorista",
                table: "Corridas",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvisosMotoristasEnviados",
                table: "Corridas");

            migrationBuilder.DropColumn(
                name: "DataDisponivel",
                table: "Corridas");

            migrationBuilder.DropColumn(
                name: "PercentualMotorista",
                table: "Corridas");
        }
    }
}
