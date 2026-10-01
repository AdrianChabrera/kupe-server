using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KupeServer.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "country",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    iso2 = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    iso3 = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    capital = table.Column<string>(type: "text", nullable: true),
                    continent = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    area_km2 = table.Column<double>(type: "double precision", nullable: true),
                    population = table.Column<long>(type: "bigint", nullable: true),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    currency_name = table.Column<string>(type: "text", nullable: true),
                    phone_prefix = table.Column<string>(type: "text", nullable: true),
                    languages = table.Column<string>(type: "text", nullable: true),
                    geoname_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<int>(type: "integer", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: true),
                    refresh_token_expiry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "place",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    geoname_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    ascii_name = table.Column<string>(type: "text", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin1_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    feature_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    population = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    elevation = table.Column<int>(type: "integer", nullable: true),
                    timezone = table.Column<string>(type: "text", nullable: true),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lon = table.Column<double>(type: "double precision", nullable: false),
                    wikidata_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place", x => x.id);
                    table.ForeignKey(
                        name: "fk_place_country_country_id",
                        column: x => x.country_id,
                        principalTable: "country",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_identities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "place_name",
                columns: table => new
                {
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_preferred = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place_name", x => new { x.place_id, x.language, x.name });
                    table.ForeignKey(
                        name: "fk_place_name_place_place_id",
                        column: x => x.place_id,
                        principalTable: "place",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_country_geoname_id",
                table: "country",
                column: "geoname_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_country_iso2",
                table: "country",
                column: "iso2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_identities_provider_provider_user_id",
                table: "external_identities",
                columns: new[] { "provider", "provider_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_identities_user_id_provider",
                table: "external_identities",
                columns: new[] { "user_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_place_admin1_code",
                table: "place",
                column: "admin1_code");

            migrationBuilder.CreateIndex(
                name: "ix_place_ascii_name",
                table: "place",
                column: "ascii_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_place_country_id_population",
                table: "place",
                columns: new[] { "country_id", "population" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_place_geoname_id",
                table: "place",
                column: "geoname_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_place_lat_lon",
                table: "place",
                columns: new[] { "lat", "lon" });

            migrationBuilder.CreateIndex(
                name: "ix_place_name",
                table: "place",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_place_name_language",
                table: "place_name",
                column: "language");

            migrationBuilder.CreateIndex(
                name: "ix_place_name_name",
                table: "place_name",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_identities");

            migrationBuilder.DropTable(
                name: "place_name");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "place");

            migrationBuilder.DropTable(
                name: "country");
        }
    }
}
