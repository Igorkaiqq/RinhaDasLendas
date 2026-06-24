using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    public partial class AddDraftMontagemRealtime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "modo", table: "draft_montagens", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Manual");
            migrationBuilder.AddColumn<Guid>(name: "turno_atual_time_id", table: "draft_montagens", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "turno_atual_capitao_id", table: "draft_montagens", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<int>(name: "turno_sequencia", table: "draft_montagens", type: "integer", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "turno_iniciado_em", table: "draft_montagens", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "turno_expira_em", table: "draft_montagens", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<int>(name: "duracao_turno_segundos", table: "draft_montagens", type: "integer", nullable: false, defaultValue: 30);
            migrationBuilder.AddColumn<long>(name: "versao_estado", table: "draft_montagens", type: "bigint", nullable: false, defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "draft_montagem_escolhas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequencia = table.Column<int>(type: "integer", nullable: false),
                    time_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capitao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jogador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagem_escolhas", x => x.id);
                    table.ForeignKey("FK_draft_montagem_escolhas_draft_montagem_times_time_id", x => x.time_id, "draft_montagem_times", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_draft_montagem_escolhas_draft_montagens_draft_montagem_id", x => x.draft_montagem_id, "draft_montagens", "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_draft_montagem_escolhas_jogadores_capitao_id", x => x.capitao_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_draft_montagem_escolhas_jogadores_jogador_id", x => x.jogador_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "draft_montagem_substituicoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jogador_saiu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserva_entrou_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    responsavel_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagem_substituicoes", x => x.id);
                    table.ForeignKey("FK_draft_montagem_substituicoes_draft_montagem_times_time_id", x => x.time_id, "draft_montagem_times", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_draft_montagem_substituicoes_draft_montagens_draft_montagem_id", x => x.draft_montagem_id, "draft_montagens", "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_draft_montagem_substituicoes_jogadores_jogador_saiu_id", x => x.jogador_saiu_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_draft_montagem_substituicoes_jogadores_reserva_entrou_id", x => x.reserva_entrou_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_draft_montagem_substituicoes_usuarios_responsavel_usuario_id", x => x.responsavel_usuario_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_draft_montagens_status_modo_turno_expira_em", table: "draft_montagens", columns: new[] { "status", "modo", "turno_expira_em" });
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_escolhas_capitao_id", table: "draft_montagem_escolhas", column: "capitao_id");
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_escolhas_draft_montagem_id_sequencia", table: "draft_montagem_escolhas", columns: new[] { "draft_montagem_id", "sequencia" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_escolhas_jogador_id", table: "draft_montagem_escolhas", column: "jogador_id");
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_escolhas_time_id", table: "draft_montagem_escolhas", column: "time_id");
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_substituicoes_draft_montagem_id", table: "draft_montagem_substituicoes", column: "draft_montagem_id");
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_substituicoes_jogador_saiu_id", table: "draft_montagem_substituicoes", column: "jogador_saiu_id");
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_substituicoes_reserva_entrou_id", table: "draft_montagem_substituicoes", column: "reserva_entrou_id");
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_substituicoes_responsavel_usuario_id", table: "draft_montagem_substituicoes", column: "responsavel_usuario_id");
            migrationBuilder.CreateIndex(name: "IX_draft_montagem_substituicoes_time_id", table: "draft_montagem_substituicoes", column: "time_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "draft_montagem_escolhas");
            migrationBuilder.DropTable(name: "draft_montagem_substituicoes");
            migrationBuilder.DropIndex(name: "IX_draft_montagens_status_modo_turno_expira_em", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "modo", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "turno_atual_time_id", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "turno_atual_capitao_id", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "turno_sequencia", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "turno_iniciado_em", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "turno_expira_em", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "duracao_turno_segundos", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "versao_estado", table: "draft_montagens");
        }
    }
}
