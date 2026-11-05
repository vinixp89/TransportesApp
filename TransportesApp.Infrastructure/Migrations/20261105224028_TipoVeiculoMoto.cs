using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TipoVeiculoMoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoVeiculo",
                table: "PacotesCorridas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoVeiculo",
                table: "Motoristas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoVeiculo",
                table: "Corridas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoVeiculo",
                table: "PacotesCorridas");

            migrationBuilder.DropColumn(
                name: "TipoVeiculo",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "TipoVeiculo",
                table: "Corridas");
        }
    }
}
