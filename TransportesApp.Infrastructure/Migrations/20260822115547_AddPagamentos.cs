using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssinaturasPlano_ClienteId",
                table: "AssinaturasPlano");

            migrationBuilder.DropColumn(
                name: "Ativa",
                table: "AssinaturasPlano");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AssinaturasPlano",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoReferencia = table.Column<int>(type: "integer", nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PagamentoGatewayId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasPlano_ClienteId",
                table: "AssinaturasPlano",
                column: "ClienteId",
                unique: true,
                filter: "\"Status\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_PagamentoGatewayId",
                table: "Pagamentos",
                column: "PagamentoGatewayId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_TipoReferencia_ReferenciaId",
                table: "Pagamentos",
                columns: new[] { "TipoReferencia", "ReferenciaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagamentos");

            migrationBuilder.DropIndex(
                name: "IX_AssinaturasPlano_ClienteId",
                table: "AssinaturasPlano");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AssinaturasPlano");

            migrationBuilder.AddColumn<bool>(
                name: "Ativa",
                table: "AssinaturasPlano",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasPlano_ClienteId",
                table: "AssinaturasPlano",
                column: "ClienteId",
                unique: true,
                filter: "\"Ativa\" = true");
        }
    }
}
