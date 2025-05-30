using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuffmanFileCompressor.App.Migrations
{
    /// <inheritdoc />
    public partial class FixError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Archives",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Modified" },
                values: new object[] { new DateTime(2025, 5, 27, 15, 10, 45, 0, DateTimeKind.Unspecified), new DateTime(2025, 5, 27, 15, 10, 45, 0, DateTimeKind.Unspecified) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Archives",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Modified" },
                values: new object[] { new DateTime(2025, 5, 27, 0, 3, 48, 470, DateTimeKind.Local).AddTicks(4436), new DateTime(2025, 5, 27, 0, 3, 48, 472, DateTimeKind.Local).AddTicks(3987) });
        }
    }
}
