using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_entries_irrigations_GardenId_EntityKey",
                table: "audit_entries");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .Annotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .Annotation("Npgsql:Enum:record_validity", "invalid,valid")
                .Annotation("Npgsql:Enum:volume_source", "estimated,measured")
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .OldAnnotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .OldAnnotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .OldAnnotation("Npgsql:Enum:record_validity", "invalid,valid")
                .OldAnnotation("Npgsql:Enum:volume_source", "estimated,measured");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "participants",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "participants",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "irrigation_sectors",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "irrigation_sectors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "irrigation_sectors",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "ExperimentalAreaM2",
                table: "gardens",
                type: "numeric(14,3)",
                precision: 14,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstitutionalAreaM2",
                table: "gardens",
                type: "numeric(14,3)",
                precision: 14,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "gardens",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OperationalAreaM2",
                table: "gardens",
                type: "numeric(14,3)",
                precision: 14,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "gardens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "gardens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "beds",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "beds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "beds",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "GardenId",
                table: "audit_entries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "IrrigationId",
                table: "audit_entries",
                type: "uuid",
                nullable: true);

            // Preserva a FK concreta de todas as auditorias existentes ao ampliar o catálogo auditável.
            migrationBuilder.Sql("UPDATE audit_entries SET \"IrrigationId\" = \"EntityKey\" WHERE \"EntityType\" = 'Irrigation'");

            migrationBuilder.CreateTable(
                name: "bed_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    LengthMeters = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    WidthMeters = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    CultivableAreaM2 = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bed_configurations", x => x.Id);
                    table.UniqueConstraint("AK_bed_configurations_GardenId_BedId_Id", x => new { x.GardenId, x.BedId, x.Id });
                    table.CheckConstraint("ck_bed_configurations_area", "\"CultivableAreaM2\" > 0 AND (\"LengthMeters\" IS NULL OR \"LengthMeters\" > 0) AND (\"WidthMeters\" IS NULL OR \"WidthMeters\" > 0)");
                    table.CheckConstraint("ck_bed_configurations_period", "\"ValidUntil\" IS NULL OR \"ValidUntil\" > \"ValidFrom\"");
                    table.ForeignKey(
                        name: "FK_bed_configurations_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_GardenId_IrrigationId",
                table: "audit_entries",
                columns: new[] { "GardenId", "IrrigationId" });

            migrationBuilder.CreateIndex(
                name: "IX_bed_configurations_BedId_ValidFrom",
                table: "bed_configurations",
                columns: new[] { "BedId", "ValidFrom" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_entries_irrigations_GardenId_IrrigationId",
                table: "audit_entries",
                columns: new[] { "GardenId", "IrrigationId" },
                principalTable: "irrigations",
                principalColumns: new[] { "GardenId", "Id" },
                onDelete: ReferentialAction.Restrict);

            // EF não modela exclusion constraints; reforçam a vigência [início, fim) no PostgreSQL.
            migrationBuilder.Sql("""
                ALTER TABLE bed_sector_assignments ADD CONSTRAINT ex_bed_sector_assignments_period
                EXCLUDE USING gist ("BedId" WITH =, daterange("ValidFrom", "ValidUntil", '[)') WITH &&);
                ALTER TABLE bed_configurations ADD CONSTRAINT ex_bed_configurations_period
                EXCLUDE USING gist ("BedId" WITH =, daterange("ValidFrom", "ValidUntil", '[)') WITH &&);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A versão anterior não suporta auditorias de cadastros; não descartar história silenciosamente.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM audit_entries WHERE "EntityType" <> 'Irrigation') THEN
                    RAISE EXCEPTION 'Rollback requer estratégia explícita para preservar as auditorias de cadastros';
                  END IF;
                END $$;
                ALTER TABLE bed_sector_assignments DROP CONSTRAINT ex_bed_sector_assignments_period;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_audit_entries_irrigations_GardenId_IrrigationId",
                table: "audit_entries");

            migrationBuilder.DropTable(
                name: "bed_configurations");

            migrationBuilder.DropIndex(
                name: "IX_audit_entries_GardenId_IrrigationId",
                table: "audit_entries");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "irrigation_sectors");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "irrigation_sectors");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "irrigation_sectors");

            migrationBuilder.DropColumn(
                name: "ExperimentalAreaM2",
                table: "gardens");

            migrationBuilder.DropColumn(
                name: "InstitutionalAreaM2",
                table: "gardens");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "gardens");

            migrationBuilder.DropColumn(
                name: "OperationalAreaM2",
                table: "gardens");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "gardens");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "gardens");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "beds");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "beds");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "beds");

            migrationBuilder.DropColumn(
                name: "IrrigationId",
                table: "audit_entries");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .Annotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .Annotation("Npgsql:Enum:record_validity", "invalid,valid")
                .Annotation("Npgsql:Enum:volume_source", "estimated,measured")
                .OldAnnotation("Npgsql:Enum:audit_operation", "corrected,created,invalidated")
                .OldAnnotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .OldAnnotation("Npgsql:Enum:record_validity", "invalid,valid")
                .OldAnnotation("Npgsql:Enum:volume_source", "estimated,measured")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.AlterColumn<Guid>(
                name: "GardenId",
                table: "audit_entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_entries_irrigations_GardenId_EntityKey",
                table: "audit_entries",
                columns: new[] { "GardenId", "EntityKey" },
                principalTable: "irrigations",
                principalColumns: new[] { "GardenId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
