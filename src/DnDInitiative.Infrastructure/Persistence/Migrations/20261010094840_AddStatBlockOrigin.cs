using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnDInitiative.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatBlockOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "StatBlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "StatBlocks");
        }
    }
}
