﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace Morgana.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildAdmins",
                columns: table => new
                {
                    GuildAdminId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    AdminId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildAdmins", x => x.GuildAdminId);
                });

            migrationBuilder.CreateTable(
                name: "GuildBadwords",
                columns: table => new
                {
                    GuildBadwordId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Badword = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBadwords", x => x.GuildBadwordId);
                });

            migrationBuilder.CreateTable(
                name: "GuildManagedRoles",
                columns: table => new
                {
                    GuildManagedroleId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    RoleId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildManagedRoles", x => x.GuildManagedroleId);
                });

            migrationBuilder.CreateTable(
                name: "GuildOptions",
                columns: table => new
                {
                    GuildOptionId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Option = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildOptions", x => x.GuildOptionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildAdmins_GuildId_AdminId",
                table: "GuildAdmins",
                columns: new[] { "GuildId", "AdminId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildBadwords_GuildId_Badword",
                table: "GuildBadwords",
                columns: new[] { "GuildId", "Badword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildManagedRoles_GuildId_RoleId",
                table: "GuildManagedRoles",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildOptions_GuildId_Option",
                table: "GuildOptions",
                columns: new[] { "GuildId", "Option" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildAdmins");

            migrationBuilder.DropTable(
                name: "GuildBadwords");

            migrationBuilder.DropTable(
                name: "GuildManagedRoles");

            migrationBuilder.DropTable(
                name: "GuildOptions");
        }
    }
}
