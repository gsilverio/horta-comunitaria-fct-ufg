using System;
using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HortaComunitaria.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAuthentication : Migration
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
                .Annotation("Npgsql:Enum:user_profile", "administrator,operator")
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

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemonstration",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<UserProfile>(
                name: "Profile",
                table: "users",
                type: "user_profile",
                nullable: false,
                defaultValue: UserProfile.Operator);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            // Ajuste manual: o autor técnico do protótipo vira linha de demonstração, inativa e sem credenciais,
            // antes da remoção do identificador textual que o distinguia; contas existentes recebem o instante da migração.
            migrationBuilder.Sql("""
                UPDATE users SET "IsDemonstration" = ("AuthenticationIdentifier" = 'demo:operator'), "CreatedAt" = now();
                UPDATE users SET "IsActive" = FALSE WHERE "IsDemonstration";
                """);

            migrationBuilder.DropIndex(
                name: "IX_users_AuthenticationIdentifier",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AuthenticationIdentifier",
                table: "users");

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_claims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_user_logins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_user_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_demonstration_without_credentials",
                table: "users",
                sql: "NOT \"IsDemonstration\" OR (\"PasswordHash\" IS NULL AND NOT \"IsActive\")");

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_UserId",
                table: "user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_UserId",
                table: "user_logins",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationIdentifier",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_demonstration_without_credentials",
                table: "users");

            // Ajuste manual: restaura o identificador do autor técnico e um valor único para as demais contas.
            migrationBuilder.Sql("""
                UPDATE users SET "AuthenticationIdentifier" = CASE WHEN "IsDemonstration" THEN 'demo:operator' ELSE 'identity:' || "Id"::text END;
                UPDATE users SET "IsActive" = TRUE WHERE "IsDemonstration";
                """);

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "users");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsDemonstration",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Profile",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "users");

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
                .OldAnnotation("Npgsql:Enum:cultivation_status", "closed,in_progress")
                .OldAnnotation("Npgsql:Enum:drying_status", "completed,in_progress")
                .OldAnnotation("Npgsql:Enum:financial_nature", "actual,planned")
                .OldAnnotation("Npgsql:Enum:irrigation_status", "completed,interrupted")
                .OldAnnotation("Npgsql:Enum:material_type", "cutting,other,seed,seedling")
                .OldAnnotation("Npgsql:Enum:measurement_quality", "normal,questionable")
                .OldAnnotation("Npgsql:Enum:occurrence_status", "in_progress,open,resolved")
                .OldAnnotation("Npgsql:Enum:planting_kind", "initial,replacement")
                .OldAnnotation("Npgsql:Enum:record_validity", "invalid,valid")
                .OldAnnotation("Npgsql:Enum:user_profile", "administrator,operator")
                .OldAnnotation("Npgsql:Enum:volume_source", "estimated,measured")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_users_AuthenticationIdentifier",
                table: "users",
                column: "AuthenticationIdentifier",
                unique: true);
        }
    }
}
