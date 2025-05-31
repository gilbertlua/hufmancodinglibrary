using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuffmanFileCompressor.App.Migrations
{
    /// <inheritdoc />
    public partial class Addextensiontomodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Extension",
                table: "Archives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Archives",
                keyColumn: "Id",
                keyValue: 1,
                column: "Extension",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Extension",
                table: "Archives");
        }
    }
}
