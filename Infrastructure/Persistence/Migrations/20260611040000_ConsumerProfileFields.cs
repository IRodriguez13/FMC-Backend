using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fmc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsumerProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarStorageKey",
                table: "ConsumerUsers",
                type: "TEXT",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ConsumerUsers",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarStorageKey",
                table: "ConsumerUsers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ConsumerUsers");
        }
    }
}
