using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace TourPlanner.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Tours",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple',\r\n    coalesce(\"Name\", '') || ' ' ||\r\n    coalesce(\"Description\", '') || ' ' ||\r\n    coalesce(\"From\", '') || ' ' ||\r\n    coalesce(\"To\", '') || ' ' ||\r\n    coalesce(\"TransportType\", '') || ' ' ||\r\n    coalesce(\"Status\", ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "TourLogs",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple',\r\n    coalesce(\"Comment\", '') || ' ' ||\r\n    coalesce(\"Difficulty\", '') || ' ' ||\r\n    \"Rating\"::text)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_SearchVector",
                table: "Tours",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_TourLogs_SearchVector",
                table: "TourLogs",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tours_SearchVector",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_TourLogs_SearchVector",
                table: "TourLogs");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "TourLogs");
        }
    }
}
