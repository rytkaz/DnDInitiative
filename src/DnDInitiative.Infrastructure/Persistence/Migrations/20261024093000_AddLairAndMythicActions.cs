using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnDInitiative.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLairAndMythicActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LairActionsDescription",
                table: "StatBlocks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LairActionsJson",
                table: "StatBlocks",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "LegendaryActionsDescription",
                table: "StatBlocks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MythicActionsDescription",
                table: "StatBlocks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MythicActionsJson",
                table: "StatBlocks",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LairActionsDescription",
                table: "StatBlocks");

            migrationBuilder.DropColumn(
                name: "LairActionsJson",
                table: "StatBlocks");

            migrationBuilder.DropColumn(
                name: "LegendaryActionsDescription",
                table: "StatBlocks");

            migrationBuilder.DropColumn(
                name: "MythicActionsDescription",
                table: "StatBlocks");

            migrationBuilder.DropColumn(
                name: "MythicActionsJson",
                table: "StatBlocks");
        }
    }
}
