using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messentra.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportedMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<long>(type: "INTEGER", nullable: false),
                    Position = table.Column<long>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    IsSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedMessages_JobId_IsSent_Position",
                table: "ImportedMessages",
                columns: new[] { "JobId", "IsSent", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedMessages_JobId_Position",
                table: "ImportedMessages",
                columns: new[] { "JobId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedMessages");
        }
    }
}
