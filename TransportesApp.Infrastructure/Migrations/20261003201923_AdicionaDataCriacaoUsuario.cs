using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaDataCriacaoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Contas existentes (de antes dessa coluna existir) recebem o valor mínimo possível de
            // propósito — isso as torna "infinitamente velhas" pro LimpezaCadastrosOrfaosService, que
            // já pode limpar qualquer uma delas que esteja órfã logo na primeira execução depois do
            // deploy, sem precisar de um backfill manual separado.
            migrationBuilder.AddColumn<DateTime>(
                name: "DataCriacao",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataCriacao",
                table: "AspNetUsers");
        }
    }
}
