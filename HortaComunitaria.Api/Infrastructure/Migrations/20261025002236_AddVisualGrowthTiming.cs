using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualGrowthTiming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VisualGrowthDays",
                table: "species",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisualHarvestDays",
                table: "species",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisualGrowthDays",
                table: "cultivars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisualHarvestDays",
                table: "cultivars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_species_visual_timing",
                table: "species",
                sql: "(\"VisualGrowthDays\" IS NULL AND \"VisualHarvestDays\" IS NULL) OR (\"VisualGrowthDays\" IS NOT NULL AND \"VisualHarvestDays\" IS NOT NULL AND \"VisualGrowthDays\" > 0 AND \"VisualHarvestDays\" > \"VisualGrowthDays\")");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cultivar_visual_timing",
                table: "cultivars",
                sql: "(\"VisualGrowthDays\" IS NULL AND \"VisualHarvestDays\" IS NULL) OR (\"VisualGrowthDays\" IS NOT NULL AND \"VisualHarvestDays\" IS NOT NULL AND \"VisualGrowthDays\" > 0 AND \"VisualHarvestDays\" > \"VisualGrowthDays\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_species_visual_timing",
                table: "species");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cultivar_visual_timing",
                table: "cultivars");

            migrationBuilder.DropColumn(
                name: "VisualGrowthDays",
                table: "species");

            migrationBuilder.DropColumn(
                name: "VisualHarvestDays",
                table: "species");

            migrationBuilder.DropColumn(
                name: "VisualGrowthDays",
                table: "cultivars");

            migrationBuilder.DropColumn(
                name: "VisualHarvestDays",
                table: "cultivars");
        }
    }
}
