using System;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Occurrences;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MvpManualRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .Annotation("Npgsql:Enum:cultivation_status", "closed,in_progress")
                .Annotation("Npgsql:Enum:drying_status", "completed,in_progress")
                .Annotation("Npgsql:Enum:financial_nature", "actual,planned")
                .Annotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .Annotation("Npgsql:Enum:material_type", "cutting,other,seed,seedling")
                .Annotation("Npgsql:Enum:measurement_quality", "normal,questionable")
                .Annotation("Npgsql:Enum:occurrence_status", "in_progress,open,resolved")
                .Annotation("Npgsql:Enum:planting_kind", "initial,replacement")
                .Annotation("Npgsql:Enum:record_validity", "invalid,valid")
                .Annotation("Npgsql:Enum:volume_source", "estimated,measured")
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .OldAnnotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .OldAnnotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .OldAnnotation("Npgsql:Enum:record_validity", "invalid,valid")
                .OldAnnotation("Npgsql:Enum:volume_source", "estimated,measured")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "agricultural_inputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ReferenceUnit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agricultural_inputs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "drying_lots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<DryingStatus>(type: "drying_status", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Method = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalDryMassKg = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drying_lots", x => x.Id);
                    table.UniqueConstraint("AK_drying_lots_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.ForeignKey(
                        name: "FK_drying_lots_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_drying_lots_participants_GardenId_ResponsibleParticipantId",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_drying_lots_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "environmental_variables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReferenceUnit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_environmental_variables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "institutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Acronym = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sampling_points",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Location = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BedId = table.Column<Guid>(type: "uuid", nullable: true),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sampling_points", x => x.Id);
                    table.UniqueConstraint("AK_sampling_points_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_sampling_point_context", "num_nonnulls(\"BedId\", \"SectorId\") <= 1");
                    table.ForeignKey(
                        name: "FK_sampling_points_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sampling_points_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sampling_points_irrigation_sectors_GardenId_SectorId",
                        columns: x => new { x.GardenId, x.SectorId },
                        principalTable: "irrigation_sectors",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "species",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommonName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ScientificName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_species", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "environmental_measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SamplingPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariableId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Method = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quality = table.Column<MeasurementQuality>(type: "measurement_quality", nullable: false),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BedId = table.Column<Guid>(type: "uuid", nullable: true),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    BedSectorAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_environmental_measurements", x => x.Id);
                    table.UniqueConstraint("AK_environmental_measurements_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.ForeignKey(
                        name: "FK_environmental_measurements_bed_sector_assignments_GardenId_~",
                        columns: x => new { x.GardenId, x.BedId, x.SectorId, x.BedSectorAssignmentId },
                        principalTable: "bed_sector_assignments",
                        principalColumns: new[] { "GardenId", "BedId", "IrrigationSectorId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_environmental_measurements_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_environmental_measurements_environmental_variables_Variable~",
                        column: x => x.VariableId,
                        principalTable: "environmental_variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_environmental_measurements_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_environmental_measurements_irrigation_sectors_GardenId_Sect~",
                        columns: x => new { x.GardenId, x.SectorId },
                        principalTable: "irrigation_sectors",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_environmental_measurements_participants_GardenId_Responsibl~",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_environmental_measurements_sampling_points_GardenId_Samplin~",
                        columns: x => new { x.GardenId, x.SamplingPointId },
                        principalTable: "sampling_points",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_environmental_measurements_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cultivars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpeciesId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cultivars", x => x.Id);
                    table.UniqueConstraint("AK_cultivars_SpeciesId_Id", x => new { x.SpeciesId, x.Id });
                    table.ForeignKey(
                        name: "FK_cultivars_species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cultivation_cycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpeciesId = table.Column<Guid>(type: "uuid", nullable: false),
                    CultivarId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    OccupiedAreaM2 = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BedConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceAreaM2 = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Status = table.Column<CultivationStatus>(type: "cultivation_status", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cultivation_cycles", x => x.Id);
                    table.UniqueConstraint("AK_cultivation_cycles_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_cultivation_cycles_values", "\"OccupiedAreaM2\" > 0 AND \"ReferenceAreaM2\" > 0 AND \"OccupiedAreaM2\" <= \"ReferenceAreaM2\" AND (\"EndedOn\" IS NULL OR \"EndedOn\" >= \"StartedOn\")");
                    table.ForeignKey(
                        name: "FK_cultivation_cycles_bed_configurations_GardenId_BedId_BedCon~",
                        columns: x => new { x.GardenId, x.BedId, x.BedConfigurationId },
                        principalTable: "bed_configurations",
                        principalColumns: new[] { "GardenId", "BedId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cultivation_cycles_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cultivation_cycles_cultivars_SpeciesId_CultivarId",
                        columns: x => new { x.SpeciesId, x.CultivarId },
                        principalTable: "cultivars",
                        principalColumns: new[] { "SpeciesId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cultivation_cycles_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cultivation_cycles_participants_GardenId_ResponsiblePartici~",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cultivation_cycles_species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cultivation_cycles_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "propagation_lots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SpeciesId = table.Column<Guid>(type: "uuid", nullable: false),
                    CultivarId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialType = table.Column<MaterialType>(type: "material_type", nullable: false),
                    Origin = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReceivedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_propagation_lots", x => x.Id);
                    table.UniqueConstraint("AK_propagation_lots_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.ForeignKey(
                        name: "FK_propagation_lots_cultivars_SpeciesId_CultivarId",
                        columns: x => new { x.SpeciesId, x.CultivarId },
                        principalTable: "cultivars",
                        principalColumns: new[] { "SpeciesId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_propagation_lots_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_propagation_lots_species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agronomic_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Measurement = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Method = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agronomic_observations", x => x.Id);
                    table.UniqueConstraint("AK_agronomic_observations_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.ForeignKey(
                        name: "FK_agronomic_observations_cultivation_cycles_GardenId_CycleId",
                        columns: x => new { x.GardenId, x.CycleId },
                        principalTable: "cultivation_cycles",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agronomic_observations_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agronomic_observations_participants_GardenId_ResponsiblePar~",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agronomic_observations_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "financial_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AmountBrl = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Nature = table.Column<FinancialNature>(type: "financial_nature", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: true),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_entries", x => x.Id);
                    table.UniqueConstraint("AK_financial_entries_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_financial_entries_values", "\"AmountBrl\" >= 0 AND num_nonnulls(\"BedId\", \"CycleId\") <= 1");
                    table.ForeignKey(
                        name: "FK_financial_entries_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_entries_cultivation_cycles_GardenId_CycleId",
                        columns: x => new { x.GardenId, x.CycleId },
                        principalTable: "cultivation_cycles",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_entries_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_entries_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "harvests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    HarvestedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    FreshMassKg = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    UsableMassKg = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    LossReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_harvests", x => x.Id);
                    table.UniqueConstraint("AK_harvests_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_harvests_values", "\"FreshMassKg\" >= 0 AND (\"UsableMassKg\" IS NULL OR (\"UsableMassKg\" >= 0 AND \"UsableMassKg\" <= \"FreshMassKg\"))");
                    table.ForeignKey(
                        name: "FK_harvests_cultivation_cycles_GardenId_CycleId",
                        columns: x => new { x.GardenId, x.CycleId },
                        principalTable: "cultivation_cycles",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_harvests_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_harvests_participants_GardenId_ResponsibleParticipantId",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_harvests_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "occurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<OccurrenceStatus>(type: "occurrence_status", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BedId = table.Column<Guid>(type: "uuid", nullable: true),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IrrigationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_occurrences", x => x.Id);
                    table.UniqueConstraint("AK_occurrences_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_occurrences_values", "num_nonnulls(\"BedId\", \"SectorId\", \"CycleId\", \"IrrigationId\") <= 1 AND (\"ResolvedAt\" IS NULL OR \"ResolvedAt\" >= \"OpenedAt\")");
                    table.ForeignKey(
                        name: "FK_occurrences_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_occurrences_cultivation_cycles_GardenId_CycleId",
                        columns: x => new { x.GardenId, x.CycleId },
                        principalTable: "cultivation_cycles",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_occurrences_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_occurrences_irrigation_sectors_GardenId_SectorId",
                        columns: x => new { x.GardenId, x.SectorId },
                        principalTable: "irrigation_sectors",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_occurrences_irrigations_GardenId_IrrigationId",
                        columns: x => new { x.GardenId, x.IrrigationId },
                        principalTable: "irrigations",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_occurrences_participants_GardenId_ResponsibleParticipantId",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_occurrences_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plantings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropagationLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplacedPlantingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlantedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<PlantingKind>(type: "planting_kind", nullable: false),
                    PlantedQuantity = table.Column<int>(type: "integer", nullable: false),
                    PlantSpacingCm = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    RowSpacingCm = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plantings", x => x.Id);
                    table.UniqueConstraint("AK_plantings_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_plantings_values", "\"PlantedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_plantings_cultivation_cycles_GardenId_CycleId",
                        columns: x => new { x.GardenId, x.CycleId },
                        principalTable: "cultivation_cycles",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plantings_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plantings_participants_GardenId_ResponsibleParticipantId",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plantings_plantings_GardenId_ReplacedPlantingId",
                        columns: x => new { x.GardenId, x.ReplacedPlantingId },
                        principalTable: "plantings",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plantings_propagation_lots_GardenId_PropagationLotId",
                        columns: x => new { x.GardenId, x.PropagationLotId },
                        principalTable: "propagation_lots",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plantings_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "drying_items",
                columns: table => new
                {
                    DryingLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    HarvestId = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedMassKg = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drying_items", x => new { x.DryingLotId, x.HarvestId });
                    table.CheckConstraint("ck_drying_items_value", "\"AllocatedMassKg\" > 0");
                    table.ForeignKey(
                        name: "FK_drying_items_drying_lots_GardenId_DryingLotId",
                        columns: x => new { x.GardenId, x.DryingLotId },
                        principalTable: "drying_lots",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_drying_items_harvests_GardenId_HarvestId",
                        columns: x => new { x.GardenId, x.HarvestId },
                        principalTable: "harvests",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PerformedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BedId = table.Column<Guid>(type: "uuid", nullable: true),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsOperational = table.Column<bool>(type: "boolean", nullable: false),
                    IsSocial = table.Column<bool>(type: "boolean", nullable: false),
                    IsEducational = table.Column<bool>(type: "boolean", nullable: false),
                    Objective = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Modality = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ReportedAudienceTotal = table.Column<int>(type: "integer", nullable: true),
                    AudienceListComplete = table.Column<bool>(type: "boolean", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.Id);
                    table.UniqueConstraint("AK_activities_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.ForeignKey(
                        name: "FK_activities_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_cultivation_cycles_GardenId_CycleId",
                        columns: x => new { x.GardenId, x.CycleId },
                        principalTable: "cultivation_cycles",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_irrigation_sectors_GardenId_SectorId",
                        columns: x => new { x.GardenId, x.SectorId },
                        principalTable: "irrigation_sectors",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_occurrences_GardenId_OccurrenceId",
                        columns: x => new { x.GardenId, x.OccurrenceId },
                        principalTable: "occurrences",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "establishment_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SurvivingQuantity = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponsibleParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Validity = table.Column<RecordValidity>(type: "record_validity", nullable: false),
                    CreationPayload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishment_assessments", x => x.Id);
                    table.UniqueConstraint("AK_establishment_assessments_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.CheckConstraint("ck_establishment_assessments_values", "\"SurvivingQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_establishment_assessments_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_establishment_assessments_participants_GardenId_Responsible~",
                        columns: x => new { x.GardenId, x.ResponsibleParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_establishment_assessments_plantings_GardenId_PlantingId",
                        columns: x => new { x.GardenId, x.PlantingId },
                        principalTable: "plantings",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_establishment_assessments_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "activity_institutions",
                columns: table => new
                {
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_institutions", x => new { x.ActivityId, x.InstitutionId });
                    table.ForeignKey(
                        name: "FK_activity_institutions_activities_GardenId_ActivityId",
                        columns: x => new { x.GardenId, x.ActivityId },
                        principalTable: "activities",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activity_institutions_institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "activity_participants",
                columns: table => new
                {
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    WorkHours = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    CountsAsAudience = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_participants", x => new { x.ActivityId, x.ParticipantId });
                    table.CheckConstraint("ck_activity_participants_value", "\"WorkHours\" IS NULL OR \"WorkHours\" >= 0");
                    table.ForeignKey(
                        name: "FK_activity_participants_activities_GardenId_ActivityId",
                        columns: x => new { x.GardenId, x.ActivityId },
                        principalTable: "activities",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activity_participants_participants_GardenId_ParticipantId",
                        columns: x => new { x.GardenId, x.ParticipantId },
                        principalTable: "participants",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "input_usages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_input_usages", x => x.Id);
                    table.CheckConstraint("ck_input_usages_value", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_input_usages_activities_GardenId_ActivityId",
                        columns: x => new { x.GardenId, x.ActivityId },
                        principalTable: "activities",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_input_usages_agricultural_inputs_InputId",
                        column: x => x.InputId,
                        principalTable: "agricultural_inputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activities_GardenId_BedId",
                table: "activities",
                columns: new[] { "GardenId", "BedId" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_GardenId_CycleId",
                table: "activities",
                columns: new[] { "GardenId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_GardenId_OccurrenceId",
                table: "activities",
                columns: new[] { "GardenId", "OccurrenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_GardenId_PerformedOn_Id",
                table: "activities",
                columns: new[] { "GardenId", "PerformedOn", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_activities_GardenId_SectorId",
                table: "activities",
                columns: new[] { "GardenId", "SectorId" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_RecordedByUserId",
                table: "activities",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_activities_garden_operation",
                table: "activities",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_institutions_GardenId_ActivityId",
                table: "activity_institutions",
                columns: new[] { "GardenId", "ActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_activity_institutions_InstitutionId",
                table: "activity_institutions",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_participants_GardenId_ActivityId",
                table: "activity_participants",
                columns: new[] { "GardenId", "ActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_activity_participants_GardenId_ParticipantId",
                table: "activity_participants",
                columns: new[] { "GardenId", "ParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_agronomic_observations_GardenId_CycleId",
                table: "agronomic_observations",
                columns: new[] { "GardenId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_agronomic_observations_GardenId_ObservedOn_Id",
                table: "agronomic_observations",
                columns: new[] { "GardenId", "ObservedOn", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_agronomic_observations_GardenId_ResponsibleParticipantId",
                table: "agronomic_observations",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_agronomic_observations_RecordedByUserId",
                table: "agronomic_observations",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_agronomic_observations_garden_operation",
                table: "agronomic_observations",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cultivation_cycles_GardenId_BedId_BedConfigurationId",
                table: "cultivation_cycles",
                columns: new[] { "GardenId", "BedId", "BedConfigurationId" });

            migrationBuilder.CreateIndex(
                name: "IX_cultivation_cycles_GardenId_ResponsibleParticipantId",
                table: "cultivation_cycles",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_cultivation_cycles_GardenId_StartedOn_Id",
                table: "cultivation_cycles",
                columns: new[] { "GardenId", "StartedOn", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_cultivation_cycles_RecordedByUserId",
                table: "cultivation_cycles",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cultivation_cycles_SpeciesId_CultivarId",
                table: "cultivation_cycles",
                columns: new[] { "SpeciesId", "CultivarId" });

            migrationBuilder.CreateIndex(
                name: "ux_cultivation_cycles_garden_operation",
                table: "cultivation_cycles",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drying_items_GardenId_DryingLotId",
                table: "drying_items",
                columns: new[] { "GardenId", "DryingLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_drying_items_GardenId_HarvestId",
                table: "drying_items",
                columns: new[] { "GardenId", "HarvestId" });

            migrationBuilder.CreateIndex(
                name: "IX_drying_lots_GardenId_Code",
                table: "drying_lots",
                columns: new[] { "GardenId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drying_lots_GardenId_ResponsibleParticipantId",
                table: "drying_lots",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_drying_lots_GardenId_StartedOn_Id",
                table: "drying_lots",
                columns: new[] { "GardenId", "StartedOn", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_drying_lots_RecordedByUserId",
                table: "drying_lots",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_drying_lots_garden_operation",
                table: "drying_lots",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_environmental_measurements_GardenId_BedId_SectorId_BedSecto~",
                table: "environmental_measurements",
                columns: new[] { "GardenId", "BedId", "SectorId", "BedSectorAssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_environmental_measurements_GardenId_CollectedAt_Id",
                table: "environmental_measurements",
                columns: new[] { "GardenId", "CollectedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_environmental_measurements_GardenId_ResponsibleParticipantId",
                table: "environmental_measurements",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_environmental_measurements_GardenId_SamplingPointId",
                table: "environmental_measurements",
                columns: new[] { "GardenId", "SamplingPointId" });

            migrationBuilder.CreateIndex(
                name: "IX_environmental_measurements_GardenId_SectorId",
                table: "environmental_measurements",
                columns: new[] { "GardenId", "SectorId" });

            migrationBuilder.CreateIndex(
                name: "IX_environmental_measurements_RecordedByUserId",
                table: "environmental_measurements",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_environmental_measurements_VariableId",
                table: "environmental_measurements",
                column: "VariableId");

            migrationBuilder.CreateIndex(
                name: "ux_environmental_measurements_garden_operation",
                table: "environmental_measurements",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_environmental_variables_Code",
                table: "environmental_variables",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_establishment_assessments_GardenId_AssessedOn_Id",
                table: "establishment_assessments",
                columns: new[] { "GardenId", "AssessedOn", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_establishment_assessments_GardenId_PlantingId",
                table: "establishment_assessments",
                columns: new[] { "GardenId", "PlantingId" });

            migrationBuilder.CreateIndex(
                name: "IX_establishment_assessments_GardenId_ResponsibleParticipantId",
                table: "establishment_assessments",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_establishment_assessments_RecordedByUserId",
                table: "establishment_assessments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_establishment_assessments_garden_operation",
                table: "establishment_assessments",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_entries_GardenId_BedId",
                table: "financial_entries",
                columns: new[] { "GardenId", "BedId" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_entries_GardenId_CycleId",
                table: "financial_entries",
                columns: new[] { "GardenId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_entries_GardenId_EntryDate_Id",
                table: "financial_entries",
                columns: new[] { "GardenId", "EntryDate", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_financial_entries_RecordedByUserId",
                table: "financial_entries",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_financial_entries_garden_operation",
                table: "financial_entries",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_harvests_GardenId_CycleId",
                table: "harvests",
                columns: new[] { "GardenId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_harvests_GardenId_HarvestedOn_Id",
                table: "harvests",
                columns: new[] { "GardenId", "HarvestedOn", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_harvests_GardenId_ResponsibleParticipantId",
                table: "harvests",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_harvests_RecordedByUserId",
                table: "harvests",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_harvests_garden_operation",
                table: "harvests",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_input_usages_GardenId_ActivityId",
                table: "input_usages",
                columns: new[] { "GardenId", "ActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_input_usages_InputId",
                table: "input_usages",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_occurrences_GardenId_BedId",
                table: "occurrences",
                columns: new[] { "GardenId", "BedId" });

            migrationBuilder.CreateIndex(
                name: "IX_occurrences_GardenId_CycleId",
                table: "occurrences",
                columns: new[] { "GardenId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_occurrences_GardenId_IrrigationId",
                table: "occurrences",
                columns: new[] { "GardenId", "IrrigationId" });

            migrationBuilder.CreateIndex(
                name: "IX_occurrences_GardenId_OpenedAt_Id",
                table: "occurrences",
                columns: new[] { "GardenId", "OpenedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_occurrences_GardenId_ResponsibleParticipantId",
                table: "occurrences",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_occurrences_GardenId_SectorId",
                table: "occurrences",
                columns: new[] { "GardenId", "SectorId" });

            migrationBuilder.CreateIndex(
                name: "IX_occurrences_RecordedByUserId",
                table: "occurrences",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_occurrences_garden_operation",
                table: "occurrences",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plantings_GardenId_CycleId",
                table: "plantings",
                columns: new[] { "GardenId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_plantings_GardenId_PlantedOn_Id",
                table: "plantings",
                columns: new[] { "GardenId", "PlantedOn", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_plantings_GardenId_PropagationLotId",
                table: "plantings",
                columns: new[] { "GardenId", "PropagationLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_plantings_GardenId_ReplacedPlantingId",
                table: "plantings",
                columns: new[] { "GardenId", "ReplacedPlantingId" });

            migrationBuilder.CreateIndex(
                name: "IX_plantings_GardenId_ResponsibleParticipantId",
                table: "plantings",
                columns: new[] { "GardenId", "ResponsibleParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_plantings_RecordedByUserId",
                table: "plantings",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_plantings_garden_operation",
                table: "plantings",
                columns: new[] { "GardenId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_propagation_lots_GardenId_Code",
                table: "propagation_lots",
                columns: new[] { "GardenId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_propagation_lots_SpeciesId_CultivarId",
                table: "propagation_lots",
                columns: new[] { "SpeciesId", "CultivarId" });

            migrationBuilder.CreateIndex(
                name: "IX_sampling_points_GardenId_BedId",
                table: "sampling_points",
                columns: new[] { "GardenId", "BedId" });

            migrationBuilder.CreateIndex(
                name: "IX_sampling_points_GardenId_Code",
                table: "sampling_points",
                columns: new[] { "GardenId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sampling_points_GardenId_SectorId",
                table: "sampling_points",
                columns: new[] { "GardenId", "SectorId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_institutions");

            migrationBuilder.DropTable(
                name: "activity_participants");

            migrationBuilder.DropTable(
                name: "agronomic_observations");

            migrationBuilder.DropTable(
                name: "drying_items");

            migrationBuilder.DropTable(
                name: "environmental_measurements");

            migrationBuilder.DropTable(
                name: "establishment_assessments");

            migrationBuilder.DropTable(
                name: "financial_entries");

            migrationBuilder.DropTable(
                name: "input_usages");

            migrationBuilder.DropTable(
                name: "institutions");

            migrationBuilder.DropTable(
                name: "drying_lots");

            migrationBuilder.DropTable(
                name: "harvests");

            migrationBuilder.DropTable(
                name: "environmental_variables");

            migrationBuilder.DropTable(
                name: "sampling_points");

            migrationBuilder.DropTable(
                name: "plantings");

            migrationBuilder.DropTable(
                name: "activities");

            migrationBuilder.DropTable(
                name: "agricultural_inputs");

            migrationBuilder.DropTable(
                name: "propagation_lots");

            migrationBuilder.DropTable(
                name: "occurrences");

            migrationBuilder.DropTable(
                name: "cultivation_cycles");

            migrationBuilder.DropTable(
                name: "cultivars");

            migrationBuilder.DropTable(
                name: "species");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .Annotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .Annotation("Npgsql:Enum:record_validity", "invalid,valid")
                .Annotation("Npgsql:Enum:volume_source", "estimated,measured")
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .OldAnnotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .OldAnnotation("Npgsql:Enum:cultivation_status", "closed,in_progress")
                .OldAnnotation("Npgsql:Enum:drying_status", "completed,in_progress")
                .OldAnnotation("Npgsql:Enum:financial_nature", "actual,planned")
                .OldAnnotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .OldAnnotation("Npgsql:Enum:material_type", "cutting,other,seed,seedling")
                .OldAnnotation("Npgsql:Enum:measurement_quality", "normal,questionable")
                .OldAnnotation("Npgsql:Enum:occurrence_status", "in_progress,open,resolved")
                .OldAnnotation("Npgsql:Enum:planting_kind", "initial,replacement")
                .OldAnnotation("Npgsql:Enum:record_validity", "invalid,valid")
                .OldAnnotation("Npgsql:Enum:volume_source", "estimated,measured")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");
        }
    }
}
