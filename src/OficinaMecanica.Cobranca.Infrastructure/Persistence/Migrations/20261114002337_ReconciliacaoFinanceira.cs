using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanica.Cobranca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReconciliacaoFinanceira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsultasReconciliacao",
                table: "Pagamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciliadoEm",
                table: "Pagamentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaConsultaReconciliacaoEm",
                table: "Pagamentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsultasReconciliacao",
                table: "Estornos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciliadoEm",
                table: "Estornos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaConsultaReconciliacaoEm",
                table: "Estornos",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsultasReconciliacao",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "ReconciliadoEm",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "UltimaConsultaReconciliacaoEm",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "ConsultasReconciliacao",
                table: "Estornos");

            migrationBuilder.DropColumn(
                name: "ReconciliadoEm",
                table: "Estornos");

            migrationBuilder.DropColumn(
                name: "UltimaConsultaReconciliacaoEm",
                table: "Estornos");
        }
    }
}
