using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuffmanFileCompressor.App.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Archives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalSize = table.Column<int>(type: "INTEGER", nullable: false),
                    CompressedSize = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archives", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Archives",
                columns: new[] { "Id", "CompressedSize", "Created", "Modified", "Name", "OriginalSize", "Path" },
                values: new object[] { 1, 100, new DateTime(2025, 5, 27, 0, 3, 48, 470, DateTimeKind.Local).AddTicks(4436), new DateTime(2025, 5, 27, 0, 3, 48, 472, DateTimeKind.Local).AddTicks(3987), "Archive 1 file", 100, "Archive 1 file" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Archives");
        }
    }
}
