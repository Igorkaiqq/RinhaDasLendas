using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendamentosPresenca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agendamentos_presenca",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    horario_publicacao_local = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    horario_encerramento_local = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    ativado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    pausado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    arquivado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultima_data_avaliada = table.Column<DateOnly>(type: "date", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamentos_presenca", x => x.id);
                    table.CheckConstraint("ck_agendamentos_presenca_horarios", "horario_encerramento_local > horario_publicacao_local AND date_part('second', horario_publicacao_local) = 0 AND date_part('second', horario_encerramento_local) = 0");
                    table.CheckConstraint("ck_agendamentos_presenca_status", "status BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_agendamentos_presenca_usuarios_criado_por_usuario_id",
                        column: x => x.criado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agendamentos_presenca_dias_semana",
                columns: table => new
                {
                    agendamento_presenca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia_semana = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamentos_presenca_dias_semana", x => new { x.agendamento_presenca_id, x.dia_semana });
                    table.CheckConstraint("ck_agendamentos_presenca_dias_semana_dia", "dia_semana BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_agendamentos_presenca_dias_semana_agendamentos_presenca_age~",
                        column: x => x.agendamento_presenca_id,
                        principalTable: "agendamentos_presenca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historicos_agendamentos_presenca",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agendamento_presenca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acao = table.Column<short>(type: "smallint", nullable: false),
                    responsavel_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    campos_alterados = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historicos_agendamentos_presenca", x => x.id);
                    table.CheckConstraint("ck_historicos_agendamentos_presenca_acao", "acao BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_historicos_agendamentos_presenca_agendamentos_presenca_agen~",
                        column: x => x.agendamento_presenca_id,
                        principalTable: "agendamentos_presenca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historicos_agendamentos_presenca_usuarios_responsavel_usuar~",
                        column: x => x.responsavel_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ocorrencias_agendamentos_presenca",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agendamento_presenca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_local = table.Column<DateOnly>(type: "date", nullable: false),
                    publicacao_prevista_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    encerramento_previsto_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: true),
                    codigo_falha = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    claim_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultima_tentativa_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocorrencias_agendamentos_presenca", x => x.id);
                    table.CheckConstraint("ck_ocorrencias_agendamentos_presenca_claim", "(claim_id IS NULL AND claim_expires_at IS NULL) OR (claim_id IS NOT NULL AND claim_expires_at IS NOT NULL)");
                    table.CheckConstraint("ck_ocorrencias_agendamentos_presenca_draft_criada", "status <> 2 OR draft_montagem_id IS NOT NULL");
                    table.CheckConstraint("ck_ocorrencias_agendamentos_presenca_janela", "encerramento_previsto_em > publicacao_prevista_em");
                    table.CheckConstraint("ck_ocorrencias_agendamentos_presenca_status", "status BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_ocorrencias_agendamentos_presenca_agendamentos_presenca_age~",
                        column: x => x.agendamento_presenca_id,
                        principalTable: "agendamentos_presenca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ocorrencias_agendamentos_presenca_draft_montagens_draft_mon~",
                        column: x => x.draft_montagem_id,
                        principalTable: "draft_montagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_presenca_criado_por_usuario_id",
                table: "agendamentos_presenca",
                column: "criado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_presenca_ultima_data_avaliada_horario_publicac~",
                table: "agendamentos_presenca",
                columns: new[] { "ultima_data_avaliada", "horario_publicacao_local", "horario_encerramento_local" },
                filter: "status = 0");

            migrationBuilder.CreateIndex(
                name: "IX_historicos_agendamentos_presenca_agendamento_presenca_id",
                table: "historicos_agendamentos_presenca",
                column: "agendamento_presenca_id");

            migrationBuilder.CreateIndex(
                name: "IX_historicos_agendamentos_presenca_responsavel_usuario_id",
                table: "historicos_agendamentos_presenca",
                column: "responsavel_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_agendamentos_presenca_agendamento_presenca_id_d~",
                table: "ocorrencias_agendamentos_presenca",
                columns: new[] { "agendamento_presenca_id", "data_local" },
                unique: true,
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_agendamentos_presenca_draft_montagem_id",
                table: "ocorrencias_agendamentos_presenca",
                column: "draft_montagem_id",
                unique: true,
                filter: "draft_montagem_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_agendamentos_presenca_status_claim_expires_at_e~",
                table: "ocorrencias_agendamentos_presenca",
                columns: new[] { "status", "claim_expires_at", "encerramento_previsto_em" },
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agendamentos_presenca_dias_semana");

            migrationBuilder.DropTable(
                name: "historicos_agendamentos_presenca");

            migrationBuilder.DropTable(
                name: "ocorrencias_agendamentos_presenca");

            migrationBuilder.DropTable(
                name: "agendamentos_presenca");
        }
    }
}
