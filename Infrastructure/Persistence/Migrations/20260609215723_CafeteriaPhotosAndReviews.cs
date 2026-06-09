using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fmc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CafeteriaPhotosAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CafeteriaPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CafeteriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorRole = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CafeteriaPhotos_Cafeterias_CafeteriaId",
                        column: x => x.CafeteriaId,
                        principalTable: "Cafeterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CafeteriaReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CafeteriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorRole = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CafeteriaReviews_Cafeterias_CafeteriaId",
                        column: x => x.CafeteriaId,
                        principalTable: "Cafeterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CafeteriaPhotos_CafeteriaId",
                table: "CafeteriaPhotos",
                column: "CafeteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeteriaReviews_CafeteriaId",
                table: "CafeteriaReviews",
                column: "CafeteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeteriaReviews_CafeteriaId_AuthorUserId_AuthorRole",
                table: "CafeteriaReviews",
                columns: new[] { "CafeteriaId", "AuthorUserId", "AuthorRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CafeteriaPhotos");

            migrationBuilder.DropTable(
                name: "CafeteriaReviews");
        }
    }
}
