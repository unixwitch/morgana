using Microsoft.EntityFrameworkCore.Migrations;

namespace Morgana.Migrations
{
    public partial class maxlength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GuildId",
                table: "GuildBadwords",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "GuildId",
                table: "GuildBadwords",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 20);
        }
    }
}
