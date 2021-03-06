﻿using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Morgana.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<string>(maxLength: 20, nullable: false),
                    AdminId = table.Column<string>(maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildAdmins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildBadwords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<string>(maxLength: 20, nullable: false),
                    Badword = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBadwords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildManagedRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<string>(maxLength: 20, nullable: false),
                    RoleId = table.Column<string>(maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildManagedRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildOptions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<string>(maxLength: 20, nullable: false),
                    Option = table.Column<string>(maxLength: 64, nullable: false),
                    Value = table.Column<string>(maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildOptions", x => x.Id);
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
