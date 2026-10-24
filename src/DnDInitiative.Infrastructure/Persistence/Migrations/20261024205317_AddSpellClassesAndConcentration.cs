using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnDInitiative.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpellClassesAndConcentration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classes",
                table: "Spells",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "IsConcentration",
                table: "Spells",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LevelNumber",
                table: "Spells",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Spells already imported get their level and concentration from the text they
            // have, so the filters work for them before the next import. SQLite's CAST reads the
            // leading digits: "3rd-level" gives 3 and "cantrip" gives 0. Classes cannot be worked
            // out from what is stored, so those stay empty until the next import.
            migrationBuilder.Sql("UPDATE Spells SET LevelNumber = CAST(Level AS INTEGER);");
            migrationBuilder.Sql("UPDATE Spells SET IsConcentration = 1 WHERE Duration LIKE 'Concentration%';");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_LevelNumber",
                table: "Spells",
                column: "LevelNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spells_LevelNumber",
                table: "Spells");

            migrationBuilder.DropColumn(
                name: "Classes",
                table: "Spells");

            migrationBuilder.DropColumn(
                name: "IsConcentration",
                table: "Spells");

            migrationBuilder.DropColumn(
                name: "LevelNumber",
                table: "Spells");
        }
    }
}
