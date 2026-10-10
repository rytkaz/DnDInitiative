using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnDInitiative.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpells : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Spells",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Level = table.Column<string>(type: "TEXT", nullable: false),
                    School = table.Column<string>(type: "TEXT", nullable: false),
                    IsRitual = table.Column<bool>(type: "INTEGER", nullable: false),
                    CastingTime = table.Column<string>(type: "TEXT", nullable: false),
                    Range = table.Column<string>(type: "TEXT", nullable: false),
                    Components = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spells", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Spells_Name",
                table: "Spells",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Spells");
        }
    }
}
