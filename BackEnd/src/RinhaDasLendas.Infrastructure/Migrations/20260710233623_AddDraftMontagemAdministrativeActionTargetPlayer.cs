using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftMontagemAdministrativeActionTargetPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "jogador_alvo_id",
                table: "draft_montagem_acoes_administrativas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_acoes_administrativas_jogador_alvo_id",
                table: "draft_montagem_acoes_administrativas",
                column: "jogador_alvo_id");

            migrationBuilder.AddForeignKey(
                name: "FK_draft_montagem_acoes_administrativas_jogadores_jogador_alvo~",
                table: "draft_montagem_acoes_administrativas",
                column: "jogador_alvo_id",
                principalTable: "jogadores",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_draft_montagem_acoes_administrativas_jogadores_jogador_alvo~",
                table: "draft_montagem_acoes_administrativas");

            migrationBuilder.DropIndex(
                name: "IX_draft_montagem_acoes_administrativas_jogador_alvo_id",
                table: "draft_montagem_acoes_administrativas");

            migrationBuilder.DropColumn(
                name: "jogador_alvo_id",
                table: "draft_montagem_acoes_administrativas");
        }
    }
}
