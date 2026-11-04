using System;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IrrigationRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bed_sector_assignments_GardenId_BedId",
                table: "bed_sector_assignments");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .Annotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .Annotation("Npgsql:Enum:record_validity", "invalid,valid")
                .Annotation("Npgsql:Enum:volume_source", "estimated,measured");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_participants_GardenId_Id",
                table: "participants",
                columns: new[] { "GardenId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_bed_sector_assignments_GardenId_BedId_IrrigationSectorId_Id",
                table: "bed_sector_assignments",
                columns: new[] { "GardenId", "BedId", "IrrigationSectorId", "Id" });

            migrationBuilder.CreateTable(
                name: "irrigations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    IrrigationSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveDurationMinutes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    VolumeLiters = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    FlowRateLitersPerMinute = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    VolumeSource = table.Column<VolumeSource>(type: "volume_source", nullable: true),
                    VolumeMethod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<IrrigationStatus>(type: "irrigation_status", nullable: false),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigations", x => x.Id);
                    table.UniqueConstraint("AK_irrigations_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_irrigations_active_duration", "\"EndedAt\" IS NULL OR \"ActiveDurationMinutes\" IS NULL OR \"ActiveDurationMinutes\" <= EXTRACT(EPOCH FROM (\"EndedAt\" - \"StartedAt\")) / 60");
                    table.CheckConstraint("ck_irrigations_measures", "(\"VolumeLiters\" IS NULL OR \"VolumeLiters\" >= 0) AND (\"ActiveDurationMinutes\" IS NULL OR \"ActiveDurationMinutes\" >= 0) AND (\"FlowRateLitersPerMinute\" IS NULL OR \"FlowRateLitersPerMinute\" >= 0)");
                    table.CheckConstraint("ck_irrigations_period", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"");
                    table.CheckConstraint("ck_irrigations_version", "\"Version\" >= 1");
                    table.CheckConstraint("ck_irrigations_volume_origin", "(\"VolumeLiters\" IS NULL AND \"VolumeSource\" IS NULL AND \"VolumeMethod\" IS NULL) OR (\"VolumeLiters\" IS NOT NULL AND \"VolumeSource\" IS NOT NULL AND \"VolumeMethod\" IS NOT NULL AND length(btrim(\"VolumeMethod\")) > 0)");
                    table.ForeignKey(
                        name: "FK_irrigations_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigations_irrigation_sectors_GardenId_IrrigationSectorId",
                        columns: x => new { x.GardenId, x.IrrigationSectorId },
                        principalTable: "irrigation_sectors",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigations_participants_GardenId_ResponsibleParticipantId",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigations_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityKey = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<AuditOperation>(type: "audit_operation", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Before = table.Column<string>(type: "jsonb", nullable: true),
                    After = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_entries_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_entries_irrigations_GardenId_EntityKey",
                        columns: x => new { x.GardenId, x.EntityKey },
                        principalTable: "irrigations",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "irrigation_beds",
                columns: table => new
                {
                    IrrigationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    IrrigationSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedSectorAssignmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigation_beds", x => new { x.IrrigationId, x.BedId });
                    table.ForeignKey(
                        name: "FK_irrigation_beds_bed_sector_assignments_GardenId_BedId_Irrig~",
                        columns: x => new { x.GardenId, x.BedId, x.IrrigationSectorId, x.BedSectorAssignmentId },
                        principalTable: "bed_sector_assignments",
                        principalColumns: new[] { "GardenId", "BedId", "IrrigationSectorId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigation_beds_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigation_beds_irrigations_GardenId_IrrigationId",
                        columns: x => new { x.GardenId, x.IrrigationId },
                        principalTable: "irrigations",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_GardenId_EntityKey_OccurredAt_Id",
                table: "audit_entries",
                columns: new[] { "GardenId", "EntityKey", "OccurredAt", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_UserId",
                table: "audit_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_beds_GardenId_BedId_IrrigationSectorId_BedSector~",
                table: "irrigation_beds",
                columns: new[] { "GardenId", "BedId", "IrrigationSectorId", "BedSectorAssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_beds_GardenId_IrrigationId",
                table: "irrigation_beds",
                columns: new[] { "GardenId", "IrrigationId" });

            migrationBuilder.CreateIndex(
                name: "IX_irrigations_GardenId_IrrigationSectorId_StartedAt_Id",
                table: "irrigations",
                columns: new[] { "GardenId", "IrrigationSectorId", "StartedAt", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_irrigations_GardenId_ResponsibleParticipantId",
                table: "irrigations",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_irrigations_GardenId_StartedAt_Id",
                table: "irrigations",
                columns: new[] { "GardenId", "StartedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_irrigations_RecordedByUserId",
                table: "irrigations",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_irrigations_garden_operation",
                table: "irrigations",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "irrigation_beds");

            migrationBuilder.DropTable(
                name: "irrigations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_participants_GardenId_Id",
                table: "participants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_bed_sector_assignments_GardenId_BedId_IrrigationSectorId_Id",
                table: "bed_sector_assignments");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .OldAnnotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .OldAnnotation("Npgsql:Enum:record_validity", "invalid,valid")
                .OldAnnotation("Npgsql:Enum:volume_source", "estimated,measured");

            migrationBuilder.CreateIndex(
                name: "IX_bed_sector_assignments_GardenId_BedId",
                table: "bed_sector_assignments",
                columns: new[] { "GardenId", "BedId" });
        }
    }
}
