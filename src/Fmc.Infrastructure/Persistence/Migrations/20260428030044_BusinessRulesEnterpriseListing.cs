using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fmc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BusinessRulesEnterpriseListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HasPaidPromotion",
                table: "Cafeterias",
                newName: "ListingActive");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "EnterpriseUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "Cafeterias",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "EnterpriseUsers");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Cafeterias");

            migrationBuilder.RenameColumn(
                name: "ListingActive",
                table: "Cafeterias",
                newName: "HasPaidPromotion");
        }
    }
}
