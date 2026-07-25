using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendamentoPresencaSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nome_snapshot",
                table: "ocorrencias_agendamentos_presenca",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observacao_snapshot",
                table: "ocorrencias_agendamentos_presenca",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ocorrencias_agendamentos_presenca AS occurrence
                SET nome_snapshot = schedule.nome,
                    observacao_snapshot = schedule.observacao
                FROM agendamentos_presenca AS schedule
                WHERE schedule.id = occurrence.agendamento_presenca_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "nome_snapshot",
                table: "ocorrencias_agendamentos_presenca",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nome_snapshot",
                table: "ocorrencias_agendamentos_presenca");

            migrationBuilder.DropColumn(
                name: "observacao_snapshot",
                table: "ocorrencias_agendamentos_presenca");
        }
    }
}
