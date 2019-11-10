using Microsoft.EntityFrameworkCore.Migrations;

namespace Morgana.Migrations
{
    public partial class BadwordsRegex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRegex",
                table: "GuildBadwords",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRegex",
                table: "GuildBadwords");
        }
    }
}
