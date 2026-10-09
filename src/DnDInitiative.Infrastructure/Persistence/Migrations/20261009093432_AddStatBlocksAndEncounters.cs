using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnDInitiative.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatBlocksAndEncounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Encounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<string>(type: "TEXT", nullable: false),
                    CreatureType = table.Column<string>(type: "TEXT", nullable: false),
                    Alignment = table.Column<string>(type: "TEXT", nullable: false),
                    ArmorClass = table.Column<int>(type: "INTEGER", nullable: false),
                    HitPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    HitDice = table.Column<string>(type: "TEXT", nullable: false),
                    InitiativeModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<string>(type: "TEXT", nullable: false),
                    Strength = table.Column<int>(type: "INTEGER", nullable: false),
                    Dexterity = table.Column<int>(type: "INTEGER", nullable: false),
                    Constitution = table.Column<int>(type: "INTEGER", nullable: false),
                    Intelligence = table.Column<int>(type: "INTEGER", nullable: false),
                    Wisdom = table.Column<int>(type: "INTEGER", nullable: false),
                    Charisma = table.Column<int>(type: "INTEGER", nullable: false),
                    SavingThrows = table.Column<string>(type: "TEXT", nullable: false),
                    Skills = table.Column<string>(type: "TEXT", nullable: false),
                    DamageVulnerabilities = table.Column<string>(type: "TEXT", nullable: false),
                    DamageResistances = table.Column<string>(type: "TEXT", nullable: false),
                    DamageImmunities = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionImmunities = table.Column<string>(type: "TEXT", nullable: false),
                    Senses = table.Column<string>(type: "TEXT", nullable: false),
                    Languages = table.Column<string>(type: "TEXT", nullable: false),
                    ChallengeRating = table.Column<string>(type: "TEXT", nullable: false),
                    TraitsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ActionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    BonusActionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReactionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    LegendaryActionsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncounterCreatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EncounterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    StatBlockId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterCreatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncounterCreatures_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncounterCreatures_EncounterId",
                table: "EncounterCreatures",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_StatBlocks_Name",
                table: "StatBlocks",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncounterCreatures");

            migrationBuilder.DropTable(
                name: "StatBlocks");

            migrationBuilder.DropTable(
                name: "Encounters");
        }
    }
}
