using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotificacoesEAssinaturaExecutivoRecorrente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ClienteId",
                table: "Notificacoes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "MotoristaId",
                table: "Notificacoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreapprovalId",
                table: "AssinaturasMotoristaExecutivo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_MotoristaId_DataCriacao",
                table: "Notificacoes",
                columns: new[] { "MotoristaId", "DataCriacao" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notificacoes_Motoristas_MotoristaId",
                table: "Notificacoes",
                column: "MotoristaId",
                principalTable: "Motoristas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notificacoes_Motoristas_MotoristaId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_MotoristaId_DataCriacao",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "MotoristaId",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "PreapprovalId",
                table: "AssinaturasMotoristaExecutivo");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClienteId",
                table: "Notificacoes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
