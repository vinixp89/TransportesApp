using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaVerificacaoFacial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificacoesFaciais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorridaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MotoristaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Momento = table.Column<int>(type: "integer", nullable: false),
                    FotoUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Processada = table.Column<bool>(type: "boolean", nullable: false),
                    Similaridade = table.Column<double>(type: "double precision", nullable: true),
                    Confere = table.Column<bool>(type: "boolean", nullable: true),
                    ErroProcessamento = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DataHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificacoesFaciais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificacoesFaciais_Corridas_CorridaId",
                        column: x => x.CorridaId,
                        principalTable: "Corridas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacoesFaciais_CorridaId",
                table: "VerificacoesFaciais",
                column: "CorridaId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacoesFaciais_MotoristaId",
                table: "VerificacoesFaciais",
                column: "MotoristaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificacoesFaciais");
        }
    }
}
