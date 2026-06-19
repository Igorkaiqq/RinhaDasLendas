using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RinhaDasLendas.Infrastructure.Persistence;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations;

[DbContext(typeof(RinhaDasLendasDbContext))]
[Migration("20260619000200_CreateDrafts")]
public partial class CreateDrafts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "drafts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                tamanho_time = table.Column<int>(type: "integer", nullable: false),
                capitao_time_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                capitao_time_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                criterio_capitaes = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                proximo_time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                criterio_primeiro_pick = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_drafts", x => x.id);
                table.ForeignKey("fk_drafts_jogadores_capitao_time_a_id", x => x.capitao_time_a_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_drafts_jogadores_capitao_time_b_id", x => x.capitao_time_b_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "draft_escolhas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                draft_sessao_id = table.Column<Guid>(type: "uuid", nullable: false),
                sequencia = table.Column<int>(type: "integer", nullable: false),
                time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                capitao_id = table.Column<Guid>(type: "uuid", nullable: false),
                jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                data_escolha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_draft_escolhas", x => x.id);
                table.ForeignKey("fk_draft_escolhas_drafts_draft_sessao_id", x => x.draft_sessao_id, "drafts", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_draft_escolhas_jogadores_capitao_id", x => x.capitao_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_draft_escolhas_jogadores_jogador_id", x => x.jogador_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "draft_participantes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                draft_sessao_id = table.Column<Guid>(type: "uuid", nullable: false),
                jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                capitao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_draft_participantes", x => x.id);
                table.ForeignKey("fk_draft_participantes_drafts_draft_sessao_id", x => x.draft_sessao_id, "drafts", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_draft_participantes_jogadores_jogador_id", x => x.jogador_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("ix_drafts_capitao_time_a_id", "drafts", "capitao_time_a_id");
        migrationBuilder.CreateIndex("ix_drafts_capitao_time_b_id", "drafts", "capitao_time_b_id");
        migrationBuilder.CreateIndex("ix_drafts_data_cadastro", "drafts", "data_cadastro");
        migrationBuilder.CreateIndex("ix_drafts_status", "drafts", "status");
        migrationBuilder.CreateIndex("ix_draft_escolhas_capitao_id", "draft_escolhas", "capitao_id");
        migrationBuilder.CreateIndex("ix_draft_escolhas_jogador_id", "draft_escolhas", "jogador_id");
        migrationBuilder.CreateIndex("ix_draft_escolhas_draft_sessao_id_jogador_id", "draft_escolhas", new[] { "draft_sessao_id", "jogador_id" }, unique: true);
        migrationBuilder.CreateIndex("ix_draft_escolhas_draft_sessao_id_sequencia", "draft_escolhas", new[] { "draft_sessao_id", "sequencia" }, unique: true);
        migrationBuilder.CreateIndex("ix_draft_participantes_jogador_id", "draft_participantes", "jogador_id");
        migrationBuilder.CreateIndex("ix_draft_participantes_draft_sessao_id_jogador_id", "draft_participantes", new[] { "draft_sessao_id", "jogador_id" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "draft_escolhas");
        migrationBuilder.DropTable(name: "draft_participantes");
        migrationBuilder.DropTable(name: "drafts");
    }
}
