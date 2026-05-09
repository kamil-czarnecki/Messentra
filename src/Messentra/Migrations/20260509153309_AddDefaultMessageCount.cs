using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messentra.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultMessageCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultMessageCount",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultMessageCount",
                table: "UserSettings");
        }
    }
}
