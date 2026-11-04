using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmbrapaCatalogImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogKey",
                table: "species",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageBase64",
                table: "species",
                type: "character varying(240000)",
                maxLength: 240000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageMetadataJson",
                table: "species",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogKey",
                table: "cultivars",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_species_CatalogKey",
                table: "species",
                column: "CatalogKey",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_species_catalog_image",
                table: "species",
                sql: "(\"ImageBase64\" IS NULL AND \"ImageMetadataJson\" IS NULL) OR (\"ImageBase64\" IS NOT NULL AND \"ImageMetadataJson\" IS NOT NULL AND length(\"ImageBase64\") BETWEEN 4 AND 240000)");

            migrationBuilder.CreateIndex(
                name: "IX_cultivars_CatalogKey",
                table: "cultivars",
                column: "CatalogKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_species_CatalogKey",
                table: "species");

            migrationBuilder.DropCheckConstraint(
                name: "ck_species_catalog_image",
                table: "species");

            migrationBuilder.DropIndex(
                name: "IX_cultivars_CatalogKey",
                table: "cultivars");

            migrationBuilder.DropColumn(
                name: "CatalogKey",
                table: "species");

            migrationBuilder.DropColumn(
                name: "ImageBase64",
                table: "species");

            migrationBuilder.DropColumn(
                name: "ImageMetadataJson",
                table: "species");

            migrationBuilder.DropColumn(
                name: "CatalogKey",
                table: "cultivars");
        }
    }
}
