using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Corridas_ClienteId",
                table: "Corridas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Corridas_MotoristaId",
                table: "Corridas",
                column: "MotoristaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Corridas_Clientes_ClienteId",
                table: "Corridas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Corridas_Motoristas_MotoristaId",
                table: "Corridas",
                column: "MotoristaId",
                principalTable: "Motoristas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Corridas_Clientes_ClienteId",
                table: "Corridas");

            migrationBuilder.DropForeignKey(
                name: "FK_Corridas_Motoristas_MotoristaId",
                table: "Corridas");

            migrationBuilder.DropIndex(
                name: "IX_Corridas_ClienteId",
                table: "Corridas");

            migrationBuilder.DropIndex(
                name: "IX_Corridas_MotoristaId",
                table: "Corridas");
        }
    }
}
