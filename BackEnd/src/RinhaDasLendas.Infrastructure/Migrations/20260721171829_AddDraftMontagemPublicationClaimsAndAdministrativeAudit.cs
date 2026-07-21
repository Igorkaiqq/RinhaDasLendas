using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftMontagemPublicationClaimsAndAdministrativeAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "draft_montagem_acoes_administrativas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    responsavel_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jogador_alvo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagem_acoes_administrativas", x => x.id);
                    table.ForeignKey(
                        name: "FK_draft_montagem_acoes_administrativas_draft_montagens_draft_~",
                        column: x => x.draft_montagem_id,
                        principalTable: "draft_montagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_draft_montagem_acoes_administrativas_jogadores_jogador_alvo~",
                        column: x => x.jogador_alvo_id,
                        principalTable: "jogadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_draft_montagem_acoes_administrativas_usuarios_responsavel_u~",
                        column: x => x.responsavel_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "draft_montagem_publicacoes_discord",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    guild_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    channel_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    message_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ultimo_erro_codigo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    publicada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultima_tentativa_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    claim_expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagem_publicacoes_discord", x => x.id);
                    table.ForeignKey(
                        name: "FK_draft_montagem_publicacoes_discord_draft_montagens_draft_mo~",
                        column: x => x.draft_montagem_id,
                        principalTable: "draft_montagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_acoes_administrativas_draft_montagem_id",
                table: "draft_montagem_acoes_administrativas",
                column: "draft_montagem_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_acoes_administrativas_jogador_alvo_id",
                table: "draft_montagem_acoes_administrativas",
                column: "jogador_alvo_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_acoes_administrativas_responsavel_usuario_id",
                table: "draft_montagem_acoes_administrativas",
                column: "responsavel_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_publicacoes_discord_draft_montagem_id_tipo",
                table: "draft_montagem_publicacoes_discord",
                columns: new[] { "draft_montagem_id", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_publicacoes_discord_status",
                table: "draft_montagem_publicacoes_discord",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_publicacoes_discord_status_claim_expira_em",
                table: "draft_montagem_publicacoes_discord",
                columns: new[] { "status", "claim_expira_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_montagem_acoes_administrativas");

            migrationBuilder.DropTable(
                name: "draft_montagem_publicacoes_discord");
        }
    }
}
