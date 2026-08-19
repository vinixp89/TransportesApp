using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportesApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarEnderecoMotorista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PlacaVeiculo",
                table: "Motoristas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ModeloVeiculo",
                table: "Motoristas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CNH",
                table: "Motoristas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Endereco_Bairro",
                table: "Motoristas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Endereco_Cidade",
                table: "Motoristas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Endereco_Complemento",
                table: "Motoristas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endereco_Estado",
                table: "Motoristas",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Endereco_Latitude",
                table: "Motoristas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Endereco_Logradouro",
                table: "Motoristas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Endereco_Longitude",
                table: "Motoristas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Endereco_Numero",
                table: "Motoristas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Endereco_Bairro",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Endereco_Cidade",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Endereco_Complemento",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Endereco_Estado",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Endereco_Latitude",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Endereco_Logradouro",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Endereco_Longitude",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "Endereco_Numero",
                table: "Motoristas");

            migrationBuilder.AlterColumn<string>(
                name: "PlacaVeiculo",
                table: "Motoristas",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "ModeloVeiculo",
                table: "Motoristas",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CNH",
                table: "Motoristas",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
