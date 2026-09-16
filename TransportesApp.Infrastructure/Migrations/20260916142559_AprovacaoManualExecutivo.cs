using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AprovacaoManualExecutivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssinaturasMotoristaExecutivo_MotoristaId",
                table: "AssinaturasMotoristaExecutivo");

            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                table: "AssinaturasMotoristaExecutivo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailPagador",
                table: "AssinaturasMotoristaExecutivo",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MotivoNegacao",
                table: "AssinaturasMotoristaExecutivo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasMotoristaExecutivo_MotoristaId",
                table: "AssinaturasMotoristaExecutivo",
                column: "MotoristaId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssinaturasMotoristaExecutivo_MotoristaId",
                table: "AssinaturasMotoristaExecutivo");

            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                table: "AssinaturasMotoristaExecutivo");

            migrationBuilder.DropColumn(
                name: "EmailPagador",
                table: "AssinaturasMotoristaExecutivo");

            migrationBuilder.DropColumn(
                name: "MotivoNegacao",
                table: "AssinaturasMotoristaExecutivo");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasMotoristaExecutivo_MotoristaId",
                table: "AssinaturasMotoristaExecutivo",
                column: "MotoristaId",
                unique: true,
                filter: "\"Status\" IN (0, 1)");
        }
    }
}
