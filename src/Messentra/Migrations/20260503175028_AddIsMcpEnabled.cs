using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messentra.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMcpEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMcpEnabled",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMcpEnabled",
                table: "UserSettings");
        }
    }
}
