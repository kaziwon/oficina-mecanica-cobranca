using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanica.Cobranca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PagamentoMock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoFalha",
                table: "Pagamentos",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "Pagamentos",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "ExecucaoId",
                table: "Pagamentos",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizadoEm",
                table: "Pagamentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PedidoPagamentoId",
                table: "Pagamentos",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "ProvedorId",
                table: "Pagamentos",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximaTentativaEm",
                table: "Pagamentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SagaId",
                table: "Pagamentos",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "Tentativas",
                table: "Pagamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "Versao",
                table: "Pagamentos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_PedidoPagamentoId",
                table: "Pagamentos",
                column: "PedidoPagamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_Status_ProximaTentativaEm",
                table: "Pagamentos",
                columns: new[] { "Status", "ProximaTentativaEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pagamentos_PedidoPagamentoId",
                table: "Pagamentos");

            migrationBuilder.DropIndex(
                name: "IX_Pagamentos_Status_ProximaTentativaEm",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "CodigoFalha",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "ExecucaoId",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "FinalizadoEm",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "PedidoPagamentoId",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "ProvedorId",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "ProximaTentativaEm",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "SagaId",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "Tentativas",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "Versao",
                table: "Pagamentos");
        }
    }
}
