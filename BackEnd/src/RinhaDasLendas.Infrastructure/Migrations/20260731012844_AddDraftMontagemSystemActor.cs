using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftMontagemSystemActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "responsavel_usuario_id",
                table: "draft_montagem_acoes_administrativas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "responsavel_tipo",
                table: "draft_montagem_acoes_administrativas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddCheckConstraint(
                name: "CK_draft_montagem_acoes_administrativas_responsavel",
                table: "draft_montagem_acoes_administrativas",
                sql: "(responsavel_tipo = 'User' AND responsavel_usuario_id IS NOT NULL) OR (responsavel_tipo = 'System' AND responsavel_usuario_id IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_draft_montagem_acoes_administrativas_responsavel",
                table: "draft_montagem_acoes_administrativas");

            migrationBuilder.DropColumn(
                name: "responsavel_tipo",
                table: "draft_montagem_acoes_administrativas");

            migrationBuilder.AlterColumn<Guid>(
                name: "responsavel_usuario_id",
                table: "draft_montagem_acoes_administrativas",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
