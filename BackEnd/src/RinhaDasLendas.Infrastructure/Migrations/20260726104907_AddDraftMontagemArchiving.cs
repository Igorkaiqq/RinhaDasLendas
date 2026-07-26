using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftMontagemArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "arquivado_em",
                table: "draft_montagens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "arquivado_por_usuario_id",
                table: "draft_montagens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_arquivamento",
                table: "draft_montagens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagens_arquivado_em",
                table: "draft_montagens",
                column: "arquivado_em",
                descending: new bool[0],
                filter: "arquivado_em IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagens_arquivado_por_usuario_id",
                table: "draft_montagens",
                column: "arquivado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagens_status_data_cadastro",
                table: "draft_montagens",
                columns: new[] { "status", "data_cadastro" },
                filter: "arquivado_em IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_draft_montagens_arquivamento",
                table: "draft_montagens",
                sql: "(arquivado_em IS NULL AND arquivado_por_usuario_id IS NULL AND motivo_arquivamento IS NULL) OR (arquivado_em IS NOT NULL AND arquivado_por_usuario_id IS NOT NULL AND motivo_arquivamento IS NOT NULL AND char_length(btrim(motivo_arquivamento)) BETWEEN 1 AND 500)");

            migrationBuilder.AddForeignKey(
                name: "FK_draft_montagens_usuarios_arquivado_por_usuario_id",
                table: "draft_montagens",
                column: "arquivado_por_usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_draft_montagens_usuarios_arquivado_por_usuario_id",
                table: "draft_montagens");

            migrationBuilder.DropIndex(
                name: "IX_draft_montagens_arquivado_em",
                table: "draft_montagens");

            migrationBuilder.DropIndex(
                name: "IX_draft_montagens_arquivado_por_usuario_id",
                table: "draft_montagens");

            migrationBuilder.DropIndex(
                name: "IX_draft_montagens_status_data_cadastro",
                table: "draft_montagens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_draft_montagens_arquivamento",
                table: "draft_montagens");

            migrationBuilder.DropColumn(
                name: "arquivado_em",
                table: "draft_montagens");

            migrationBuilder.DropColumn(
                name: "arquivado_por_usuario_id",
                table: "draft_montagens");

            migrationBuilder.DropColumn(
                name: "motivo_arquivamento",
                table: "draft_montagens");
        }
    }
}
