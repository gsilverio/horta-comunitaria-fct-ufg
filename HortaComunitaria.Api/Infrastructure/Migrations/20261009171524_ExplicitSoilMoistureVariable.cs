using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExplicitSoilMoistureVariable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSoilMoisture",
                table: "environmental_variables",
                type: "boolean",
                nullable: false,
                defaultValue: false);
            // Preserva a classificação dos dados anteriores à retirada do código manual.
            migrationBuilder.Sql("UPDATE environmental_variables SET \"IsSoilMoisture\" = TRUE WHERE \"Code\" = 'soil_moisture'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSoilMoisture",
                table: "environmental_variables");
        }
    }
}
