using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fmc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsumerFavoritesAndEnterpriseCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumerFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConsumerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CafeteriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerFavorites_Cafeterias_CafeteriaId",
                        column: x => x.CafeteriaId,
                        principalTable: "Cafeterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumerFavorites_ConsumerUsers_ConsumerUserId",
                        column: x => x.ConsumerUserId,
                        principalTable: "ConsumerUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseCoupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CafeteriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    FixedAmountArs = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseCoupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnterpriseCoupons_Cafeterias_CafeteriaId",
                        column: x => x.CafeteriaId,
                        principalTable: "Cafeterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerFavorites_CafeteriaId",
                table: "ConsumerFavorites",
                column: "CafeteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerFavorites_ConsumerUserId",
                table: "ConsumerFavorites",
                column: "ConsumerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerFavorites_ConsumerUserId_CafeteriaId",
                table: "ConsumerFavorites",
                columns: new[] { "ConsumerUserId", "CafeteriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseCoupons_CafeteriaId",
                table: "EnterpriseCoupons",
                column: "CafeteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseCoupons_CafeteriaId_Code",
                table: "EnterpriseCoupons",
                columns: new[] { "CafeteriaId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumerFavorites");

            migrationBuilder.DropTable(
                name: "EnterpriseCoupons");
        }
    }
}
