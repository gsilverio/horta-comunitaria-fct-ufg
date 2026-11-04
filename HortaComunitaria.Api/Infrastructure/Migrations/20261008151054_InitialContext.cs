using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gardens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gardens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticationIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "beds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beds", x => x.Id);
                    table.UniqueConstraint("AK_beds_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.ForeignKey(
                        name: "FK_beds_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "irrigation_sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigation_sectors", x => x.Id);
                    table.UniqueConstraint("AK_irrigation_sectors_GardenId_Id", x => new { x.GardenId, x.Id });
                    table.ForeignKey(
                        name: "FK_irrigation_sectors_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_participants_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bed_sector_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedId = table.Column<Guid>(type: "uuid", nullable: false),
                    IrrigationSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bed_sector_assignments", x => x.Id);
                    table.CheckConstraint("ck_bed_sector_assignments_period", "\"ValidUntil\" IS NULL OR \"ValidUntil\" > \"ValidFrom\"");
                    table.ForeignKey(
                        name: "FK_bed_sector_assignments_beds_GardenId_BedId",
                        columns: x => new { x.GardenId, x.BedId },
                        principalTable: "beds",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bed_sector_assignments_irrigation_sectors_GardenId_Irrigati~",
                        columns: x => new { x.GardenId, x.IrrigationSectorId },
                        principalTable: "irrigation_sectors",
                        principalColumns: new[] { "GardenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bed_sector_assignments_BedId_ValidFrom",
                table: "bed_sector_assignments",
                columns: new[] { "BedId", "ValidFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bed_sector_assignments_GardenId_BedId",
                table: "bed_sector_assignments",
                columns: new[] { "GardenId", "BedId" });

            migrationBuilder.CreateIndex(
                name: "IX_bed_sector_assignments_GardenId_IrrigationSectorId_ValidFrom",
                table: "bed_sector_assignments",
                columns: new[] { "GardenId", "IrrigationSectorId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_beds_GardenId_Code",
                table: "beds",
                columns: new[] { "GardenId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gardens_Code",
                table: "gardens",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_sectors_GardenId_Code",
                table: "irrigation_sectors",
                columns: new[] { "GardenId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_participants_GardenId_DisplayName_Id",
                table: "participants",
                columns: new[] { "GardenId", "DisplayName", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_users_AuthenticationIdentifier",
                table: "users",
                column: "AuthenticationIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bed_sector_assignments");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "beds");

            migrationBuilder.DropTable(
                name: "irrigation_sectors");

            migrationBuilder.DropTable(
                name: "gardens");
        }
    }
}
