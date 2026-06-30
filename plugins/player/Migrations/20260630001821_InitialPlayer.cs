using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenarr.Plugins.Player.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerBookmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AudiobookId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionSeconds = table.Column<double>(type: "REAL", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBookmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerChapterCaches",
                columns: table => new
                {
                    AudiobookFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChaptersJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerChapterCaches", x => x.AudiobookFileId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPlaybackStates",
                columns: table => new
                {
                    AudiobookId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionSeconds = table.Column<double>(type: "REAL", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Finished = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPlaybackStates", x => x.AudiobookId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoUnmonitorOnFinish = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBookmarks_AudiobookId",
                table: "PlayerBookmarks",
                column: "AudiobookId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPlaybackStates_UpdatedUtc",
                table: "PlayerPlaybackStates",
                column: "UpdatedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerBookmarks");

            migrationBuilder.DropTable(
                name: "PlayerChapterCaches");

            migrationBuilder.DropTable(
                name: "PlayerPlaybackStates");

            migrationBuilder.DropTable(
                name: "PlayerSettings");
        }
    }
}
