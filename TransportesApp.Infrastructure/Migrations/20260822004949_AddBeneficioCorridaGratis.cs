using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficioCorridaGratis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnoMesBeneficio",
                table: "AssinaturasPlano",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BeneficioUsadoNoMes",
                table: "AssinaturasPlano",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CorridaPagaNoMes",
                table: "AssinaturasPlano",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnoMesBeneficio",
                table: "AssinaturasPlano");

            migrationBuilder.DropColumn(
                name: "BeneficioUsadoNoMes",
                table: "AssinaturasPlano");

            migrationBuilder.DropColumn(
                name: "CorridaPagaNoMes",
                table: "AssinaturasPlano");
        }
    }
}
