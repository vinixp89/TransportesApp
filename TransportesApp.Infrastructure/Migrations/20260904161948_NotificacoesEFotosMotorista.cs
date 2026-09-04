using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotificacoesEFotosMotorista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPlacaUrl",
                table: "Motoristas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoSelfieUrl",
                table: "Motoristas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoVeiculoUrl",
                table: "Motoristas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarteirasMotorista",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotoristaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Saldo = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarteirasMotorista", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarteirasMotorista_Motoristas_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Motoristas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Lida = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacoesSaque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MotoristaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    ChavePix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Banco = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Agencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Conta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    TipoConta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataProcessamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotivoRejeicao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesSaque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesSaque_Motoristas_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Motoristas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransacoesCarteiraMotorista",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarteiraMotoristaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransacoesCarteiraMotorista", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransacoesCarteiraMotorista_CarteirasMotorista_CarteiraMoto~",
                        column: x => x.CarteiraMotoristaId,
                        principalTable: "CarteirasMotorista",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasMotorista_MotoristaId",
                table: "CarteirasMotorista",
                column: "MotoristaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_ClienteId_DataCriacao",
                table: "Notificacoes",
                columns: new[] { "ClienteId", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesSaque_MotoristaId_DataSolicitacao",
                table: "SolicitacoesSaque",
                columns: new[] { "MotoristaId", "DataSolicitacao" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesSaque_Status",
                table: "SolicitacoesSaque",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TransacoesCarteiraMotorista_CarteiraMotoristaId",
                table: "TransacoesCarteiraMotorista",
                column: "CarteiraMotoristaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "SolicitacoesSaque");

            migrationBuilder.DropTable(
                name: "TransacoesCarteiraMotorista");

            migrationBuilder.DropTable(
                name: "CarteirasMotorista");

            migrationBuilder.DropColumn(
                name: "FotoPlacaUrl",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "FotoSelfieUrl",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "FotoVeiculoUrl",
                table: "Motoristas");
        }
    }
}
