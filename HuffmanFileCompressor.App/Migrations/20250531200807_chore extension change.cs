using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuffmanFileCompressor.App.Migrations
{
    /// <inheritdoc />
    public partial class choreextensionchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Extension",
                table: "Archives",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.UpdateData(
                table: "Archives",
                keyColumn: "Id",
                keyValue: 1,
                column: "Extension",
                value: "PDF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Extension",
                table: "Archives",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.UpdateData(
                table: "Archives",
                keyColumn: "Id",
                keyValue: 1,
                column: "Extension",
                value: 0);
        }
    }
}
