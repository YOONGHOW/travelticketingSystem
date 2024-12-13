using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobileWebAssignment.Migrations
{
    /// <inheritdoc />
    public partial class WebMobileDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "User");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "User",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhotoURL",
                table: "User",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PhotoURL",
                table: "User");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "User",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "User",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }
    }
}
