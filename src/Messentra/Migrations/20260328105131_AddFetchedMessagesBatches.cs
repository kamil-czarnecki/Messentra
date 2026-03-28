using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messentra.Migrations
{
    /// <inheritdoc />
    public partial class AddFetchedMessagesBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FetchedMessagesBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<long>(type: "INTEGER", nullable: false),
                    Messages = table.Column<string>(type: "TEXT", nullable: false),
                    MessagesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FetchedMessagesBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FetchedMessagesBatches_JobId_LastSequence",
                table: "FetchedMessagesBatches",
                columns: new[] { "JobId", "LastSequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FetchedMessagesBatches");
        }
    }
}
