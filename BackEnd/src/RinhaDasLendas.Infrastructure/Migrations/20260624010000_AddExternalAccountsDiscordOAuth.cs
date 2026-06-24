using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalAccountsDiscordOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    provider_user_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    unlinked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_accounts_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_auth_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    flow = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_auth_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_auth_states_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO external_accounts (id, usuario_id, provider, provider_user_id, username, display_name, avatar_url, linked_at, last_sync_at, unlinked_at)
                SELECT id, usuario_id, 'Discord', discord_user_id, discord_username, discord_global_name,
                       CASE
                           WHEN discord_avatar_hash IS NULL OR discord_avatar_hash = '' THEN NULL
                           ELSE 'https://cdn.discordapp.com/avatars/' || discord_user_id || '/' || discord_avatar_hash || '.png'
                       END,
                       vinculado_em, NULL, desvinculado_em
                FROM vinculos_discord
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_external_accounts_provider_provider_user_id",
                table: "external_accounts",
                columns: new[] { "provider", "provider_user_id" },
                unique: true,
                filter: "unlinked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_external_accounts_usuario_id",
                table: "external_accounts",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_accounts_usuario_id_provider",
                table: "external_accounts",
                columns: new[] { "usuario_id", "provider" },
                unique: true,
                filter: "unlinked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_states_expires_at",
                table: "external_auth_states",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_states_state_hash",
                table: "external_auth_states",
                column: "state_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_states_usuario_id",
                table: "external_auth_states",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "external_accounts");
            migrationBuilder.DropTable(name: "external_auth_states");
        }
    }
}
