using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BonusMotoristaLiberacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataLiberacao",
                table: "BonusMotoristas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Liberado",
                table: "BonusMotoristas",
                type: "boolean",
                nullable: false,
                // true só pra linhas que já existiam: na versão anterior o bônus era pago direto na
                // 1ª corrida, então elas já foram creditadas e não podem ser liberadas de novo.
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataLiberacao",
                table: "BonusMotoristas");

            migrationBuilder.DropColumn(
                name: "Liberado",
                table: "BonusMotoristas");
        }
    }
}
