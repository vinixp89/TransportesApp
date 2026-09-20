using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessaoUnicaETelefoneUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SessaoAtualId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAcessoEm",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Motoristas_Telefone",
                table: "Motoristas",
                column: "Telefone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Telefone",
                table: "Clientes",
                column: "Telefone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Motoristas_Telefone",
                table: "Motoristas");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Telefone",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "SessaoAtualId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UltimoAcessoEm",
                table: "AspNetUsers");
        }
    }
}
