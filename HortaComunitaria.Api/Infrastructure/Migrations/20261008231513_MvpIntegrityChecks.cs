using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MvpIntegrityChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_species_version",
                table: "species",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sampling_points_version",
                table: "sampling_points",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_propagation_lots_version",
                table: "propagation_lots",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_plantings_reference",
                table: "plantings",
                sql: "\"ReplacedPlantingId\" IS NULL OR \"ReplacedPlantingId\" <> \"Id\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_plantings_spacing",
                table: "plantings",
                sql: "(\"PlantSpacingCm\" IS NULL OR \"PlantSpacingCm\" > 0) AND (\"RowSpacingCm\" IS NULL OR \"RowSpacingCm\" > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_plantings_version",
                table: "plantings",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_occurrences_state",
                table: "occurrences",
                sql: "((\"Status\" = 'resolved' AND \"ResolvedAt\" IS NOT NULL AND \"ResolvedAt\" >= \"OpenedAt\" AND \"Resolution\" IS NOT NULL) OR (\"Status\" <> 'resolved' AND \"ResolvedAt\" IS NULL AND \"Resolution\" IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_occurrences_version",
                table: "occurrences",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_institutions_version",
                table: "institutions",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_harvests_version",
                table: "harvests",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_financial_entries_version",
                table: "financial_entries",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_establishment_assessments_version",
                table: "establishment_assessments",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_environmental_variables_version",
                table: "environmental_variables",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_environmental_measurements_version",
                table: "environmental_measurements",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_drying_lots_state",
                table: "drying_lots",
                sql: "((\"Status\" = 'completed' AND \"EndedOn\" IS NOT NULL AND \"EndedOn\" >= \"StartedOn\" AND \"TotalDryMassKg\" IS NOT NULL AND \"TotalDryMassKg\" >= 0) OR (\"Status\" = 'in_progress' AND \"EndedOn\" IS NULL AND \"TotalDryMassKg\" IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_drying_lots_version",
                table: "drying_lots",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cultivation_cycles_version",
                table: "cultivation_cycles",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cultivars_version",
                table: "cultivars",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_agronomic_observations_version",
                table: "agronomic_observations",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_agricultural_inputs_version",
                table: "agricultural_inputs",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_audience",
                table: "activities",
                sql: "\"ReportedAudienceTotal\" IS NULL OR \"ReportedAudienceTotal\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_context",
                table: "activities",
                sql: "num_nonnulls(\"BedId\", \"SectorId\", \"CycleId\") <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_dimensions",
                table: "activities",
                sql: "\"IsOperational\" OR \"IsSocial\" OR \"IsEducational\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_activities_version",
                table: "activities",
                sql: "\"Version\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_species_version",
                table: "species");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sampling_points_version",
                table: "sampling_points");

            migrationBuilder.DropCheckConstraint(
                name: "ck_propagation_lots_version",
                table: "propagation_lots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_plantings_reference",
                table: "plantings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_plantings_spacing",
                table: "plantings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_plantings_version",
                table: "plantings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_occurrences_state",
                table: "occurrences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_occurrences_version",
                table: "occurrences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_institutions_version",
                table: "institutions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_harvests_version",
                table: "harvests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_financial_entries_version",
                table: "financial_entries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_establishment_assessments_version",
                table: "establishment_assessments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_environmental_variables_version",
                table: "environmental_variables");

            migrationBuilder.DropCheckConstraint(
                name: "ck_environmental_measurements_version",
                table: "environmental_measurements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_drying_lots_state",
                table: "drying_lots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_drying_lots_version",
                table: "drying_lots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cultivation_cycles_version",
                table: "cultivation_cycles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cultivars_version",
                table: "cultivars");

            migrationBuilder.DropCheckConstraint(
                name: "ck_agronomic_observations_version",
                table: "agronomic_observations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_agricultural_inputs_version",
                table: "agricultural_inputs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_audience",
                table: "activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_context",
                table: "activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_dimensions",
                table: "activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_activities_version",
                table: "activities");
        }
    }
}
