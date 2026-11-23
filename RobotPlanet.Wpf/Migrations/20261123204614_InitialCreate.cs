using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RobotPlanet.Wpf.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EfRobotGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EfRobotGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EfWorldInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SavedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EfWorldInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EfRobots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Battery = table.Column<int>(type: "int", nullable: false),
                    Integrity = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    RobotType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EfRobots", x => x.Id);
                    table.CheckConstraint("CK_EfRobots_Battery", "[Battery] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_EfRobots_Integrity", "[Integrity] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_EfRobots_EfRobotGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "EfRobotGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EfRobotGroups_Name",
                table: "EfRobotGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EfRobots_GroupId",
                table: "EfRobots",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EfRobots");

            migrationBuilder.DropTable(
                name: "EfWorldInfo");

            migrationBuilder.DropTable(
                name: "EfRobotGroups");
        }
    }
}
