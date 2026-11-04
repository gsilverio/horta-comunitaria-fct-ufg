using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistGardenLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "garden_layouts",
                columns: table => new
                {
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Document = table.Column<string>(type: "jsonb", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SavedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_garden_layouts", x => x.GardenId);
                    table.ForeignKey(
                        name: "FK_garden_layouts_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_garden_layouts_users_SavedByUserId",
                        column: x => x.SavedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_garden_layouts_SavedByUserId",
                table: "garden_layouts",
                column: "SavedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "garden_layouts");
        }
    }
}
