using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaMecanica.Cobranca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RespostaOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "Orcamentos",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            // Recupera a correlacao dos orcamentos integrados criados antes desta versao.
            migrationBuilder.Sql("""
                UPDATE Orcamentos AS o
                JOIN Outbox AS m ON m.SagaId = o.SagaId
                    AND JSON_UNQUOTE(JSON_EXTRACT(m.Body, '$.type')) = 'OrcamentoGerado'
                    AND JSON_UNQUOTE(JSON_EXTRACT(m.Body, '$.causationId')) = o.PedidoGeracaoId
                SET o.CorrelationId = JSON_UNQUOTE(JSON_EXTRACT(m.Body, '$.correlationId'))
                WHERE o.CorrelationId IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Orcamentos");
        }
    }
}
