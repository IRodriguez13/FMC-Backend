using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fmc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarStorageKey",
                table: "EnterpriseUsers",
                type: "TEXT",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarStorageKey",
                table: "EnterpriseUsers");
        }
    }
}
