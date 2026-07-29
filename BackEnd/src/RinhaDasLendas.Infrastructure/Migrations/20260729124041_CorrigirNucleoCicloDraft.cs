using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirNucleoCicloDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "modo",
                table: "draft_montagens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<short>(
                name: "ciclo_versao",
                table: "draft_montagens",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AlterColumn<short>(
                name: "ciclo_versao",
                table: "draft_montagens",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ciclo_versao",
                table: "draft_montagens");

            migrationBuilder.AlterColumn<string>(
                name: "modo",
                table: "draft_montagens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
