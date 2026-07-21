using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftMontagemDiscordPublicationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    ultima_tentativa_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_draft_montagem_publicacoes_discord_draft_montagem_id_tipo",
                table: "draft_montagem_publicacoes_discord",
                columns: new[] { "draft_montagem_id", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_publicacoes_discord_status",
                table: "draft_montagem_publicacoes_discord",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_montagem_publicacoes_discord");
        }
    }
}
