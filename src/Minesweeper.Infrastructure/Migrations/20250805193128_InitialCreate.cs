using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minesweeper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayerId1 = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsFirstMove = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    PlayerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    DifficultyName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BoardRows = table.Column<int>(type: "INTEGER", nullable: false),
                    BoardColumns = table.Column<int>(type: "INTEGER", nullable: false),
                    MineCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    StatsTotalGames = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    StatsGamesWon = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    StatsGamesLost = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    StatsTotalPlayTime = table.Column<TimeSpan>(type: "TEXT", nullable: false, defaultValue: new TimeSpan(0, 0, 0, 0, 0)),
                    StatsBestTimeOverall = table.Column<TimeSpan>(type: "TEXT", nullable: false, defaultValue: new TimeSpan(9223372036854775807)),
                    StatsDifficultyStats = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_PlayerId",
                table: "Games",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_StartedAt",
                table: "Games",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Status",
                table: "Games",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CreatedAt",
                table: "Players",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Email",
                table: "Players",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_IsActive",
                table: "Players",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Players_LastLoginAt",
                table: "Players",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
