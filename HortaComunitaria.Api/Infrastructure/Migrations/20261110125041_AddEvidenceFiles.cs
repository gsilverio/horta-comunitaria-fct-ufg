using System;
using HortaComunitaria.Api.Domain.History;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: true),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlantingId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstablishmentAssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgronomicObservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    HarvestId = table.Column<Guid>(type: "uuid", nullable: true),
                    DryingLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CapturedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_files", x => x.Id);
                    table.CheckConstraint("ck_evidence_files_single_target", "num_nonnulls(\"BedId\", \"CycleId\", \"PlantingId\", \"EstablishmentAssessmentId\", \"AgronomicObservationId\", \"HarvestId\", \"DryingLotId\", \"ActivityId\", \"OccurrenceId\") = 1");
                    table.CheckConstraint("ck_evidence_files_size", "\"SizeBytes\" > 0");
                    table.CheckConstraint("ck_evidence_files_version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_evidence_files_activities_GardenId_ActivityId",
                        columns: x => new { x.GardenId, x.ActivityId },
                        principalTable: "activities",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_agronomic_observations_GardenId_AgronomicObs~",
                        columns: x => new { x.GardenId, x.AgronomicObservationId },
                        principalTable: "agronomic_observations",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_cultivation_cycles_GardenId_CycleId",
                        columns: x => new { x.GardenId, x.CycleId },
                        principalTable: "cultivation_cycles",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_drying_lots_GardenId_DryingLotId",
                        columns: x => new { x.GardenId, x.DryingLotId },
                        principalTable: "drying_lots",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_establishment_assessments_GardenId_Establish~",
                        columns: x => new { x.GardenId, x.EstablishmentAssessmentId },
                        principalTable: "establishment_assessments",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_harvests_GardenId_HarvestId",
                        columns: x => new { x.GardenId, x.HarvestId },
                        principalTable: "harvests",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_occurrences_GardenId_OccurrenceId",
                        columns: x => new { x.GardenId, x.OccurrenceId },
                        principalTable: "occurrences",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_plantings_GardenId_PlantingId",
                        columns: x => new { x.GardenId, x.PlantingId },
                        principalTable: "plantings",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_files_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_ActivityId",
                table: "evidence_files",
                columns: new[] { "GardenId", "ActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_AgronomicObservationId",
                table: "evidence_files",
                columns: new[] { "GardenId", "AgronomicObservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_BedId",
                table: "evidence_files",
                columns: new[] { "GardenId", "BedId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_CycleId",
                table: "evidence_files",
                columns: new[] { "GardenId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_DryingLotId",
                table: "evidence_files",
                columns: new[] { "GardenId", "DryingLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_EstablishmentAssessmentId",
                table: "evidence_files",
                columns: new[] { "GardenId", "EstablishmentAssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_HarvestId",
                table: "evidence_files",
                columns: new[] { "GardenId", "HarvestId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_OccurrenceId",
                table: "evidence_files",
                columns: new[] { "GardenId", "OccurrenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_PlantingId",
                table: "evidence_files",
                columns: new[] { "GardenId", "PlantingId" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_GardenId_RecordedAt_Id",
                table: "evidence_files",
                columns: new[] { "GardenId", "RecordedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_files_RecordedByUserId",
                table: "evidence_files",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_evidence_files_garden_operation",
                table: "evidence_files",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_evidence_files_storage_key",
                table: "evidence_files",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_files");
        }
    }
}
