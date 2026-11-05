using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MotoristaMotoDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CnhCategoriaAEar",
                table: "Motoristas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FotoCnhUrl",
                table: "Motoristas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegrasMotoAceitasEm",
                table: "Motoristas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CnhCategoriaAEar",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "FotoCnhUrl",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "RegrasMotoAceitasEm",
                table: "Motoristas");
        }
    }
}
