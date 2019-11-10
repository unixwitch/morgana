using Microsoft.EntityFrameworkCore.Migrations;

namespace Morgana.Migrations
{
    public partial class BadwordRegexIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuildBadwords_GuildId_Badword",
                table: "GuildBadwords");

            migrationBuilder.CreateIndex(
                name: "IX_GuildBadwords_GuildId_Badword_IsRegex",
                table: "GuildBadwords",
                columns: new[] { "GuildId", "Badword", "IsRegex" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuildBadwords_GuildId_Badword_IsRegex",
                table: "GuildBadwords");

            migrationBuilder.CreateIndex(
                name: "IX_GuildBadwords_GuildId_Badword",
                table: "GuildBadwords",
                columns: new[] { "GuildId", "Badword" },
                unique: true);
        }
    }
}
