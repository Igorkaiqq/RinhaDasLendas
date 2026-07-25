using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDiscordConfigurationSingleton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "singleton_key",
                table: "discord_server_configurations",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT id,
                           ROW_NUMBER() OVER (
                               ORDER BY updated_at DESC, created_at DESC, id DESC) AS position
                    FROM discord_server_configurations
                )
                DELETE FROM discord_server_configurations AS configuration
                USING ranked
                WHERE configuration.id = ranked.id
                  AND ranked.position > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_discord_server_configurations_singleton_key",
                table: "discord_server_configurations",
                column: "singleton_key",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_discord_server_configurations_singleton_key",
                table: "discord_server_configurations",
                sql: "singleton_key = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_discord_server_configurations_singleton_key",
                table: "discord_server_configurations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discord_server_configurations_singleton_key",
                table: "discord_server_configurations");

            migrationBuilder.DropColumn(
                name: "singleton_key",
                table: "discord_server_configurations");
        }
    }
}
