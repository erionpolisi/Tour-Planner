using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace TourPlanner.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsolationAndComputedStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows predate per-user ownership and therefore have no valid UserId
            // to point at. Coursework project → wipe stale rows so the new FK is satisfiable.
            // FK cascade removes the TourLogs; RefreshTokens are already user-scoped.
            migrationBuilder.Sql(@"DELETE FROM ""TourLogs"";");
            migrationBuilder.Sql(@"DELETE FROM ""Tours"";");

            migrationBuilder.AddColumn<int>(
                name: "ChildFriendliness",
                table: "Tours",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Popularity",
                table: "Tours",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Tours",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Tours",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple',\r\n    coalesce(\"Name\", '') || ' ' ||\r\n    coalesce(\"Description\", '') || ' ' ||\r\n    coalesce(\"From\", '') || ' ' ||\r\n    coalesce(\"To\", '') || ' ' ||\r\n    coalesce(\"TransportType\", '') || ' ' ||\r\n    coalesce(\"Status\", '') || ' ' ||\r\n    \"Popularity\"::text || ' ' ||\r\n    \"ChildFriendliness\"::text || ' ' ||\r\n    (CASE\r\n        WHEN \"Popularity\" <= 0 THEN 'not tried'\r\n        WHEN \"Popularity\" <= 2 THEN 'some interest'\r\n        WHEN \"Popularity\" <= 5 THEN 'popular'\r\n        ELSE 'very popular'\r\n     END) || ' ' ||\r\n    (CASE\r\n        WHEN \"ChildFriendliness\" >= 67 THEN 'great for children'\r\n        WHEN \"ChildFriendliness\" >= 34 THEN 'ok for children'\r\n        ELSE 'not suitable for children'\r\n     END))",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true,
                oldComputedColumnSql: "to_tsvector('simple',\r\n    coalesce(\"Name\", '') || ' ' ||\r\n    coalesce(\"Description\", '') || ' ' ||\r\n    coalesce(\"From\", '') || ' ' ||\r\n    coalesce(\"To\", '') || ' ' ||\r\n    coalesce(\"TransportType\", '') || ' ' ||\r\n    coalesce(\"Status\", ''))",
                oldStored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_UserId",
                table: "Tours",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_Users_UserId",
                table: "Tours",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_Users_UserId",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_UserId",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "ChildFriendliness",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tours");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Tours",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple',\r\n    coalesce(\"Name\", '') || ' ' ||\r\n    coalesce(\"Description\", '') || ' ' ||\r\n    coalesce(\"From\", '') || ' ' ||\r\n    coalesce(\"To\", '') || ' ' ||\r\n    coalesce(\"TransportType\", '') || ' ' ||\r\n    coalesce(\"Status\", ''))",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true,
                oldComputedColumnSql: "to_tsvector('simple',\r\n    coalesce(\"Name\", '') || ' ' ||\r\n    coalesce(\"Description\", '') || ' ' ||\r\n    coalesce(\"From\", '') || ' ' ||\r\n    coalesce(\"To\", '') || ' ' ||\r\n    coalesce(\"TransportType\", '') || ' ' ||\r\n    coalesce(\"Status\", '') || ' ' ||\r\n    \"Popularity\"::text || ' ' ||\r\n    \"ChildFriendliness\"::text || ' ' ||\r\n    (CASE\r\n        WHEN \"Popularity\" <= 0 THEN 'not tried'\r\n        WHEN \"Popularity\" <= 2 THEN 'some interest'\r\n        WHEN \"Popularity\" <= 5 THEN 'popular'\r\n        ELSE 'very popular'\r\n     END) || ' ' ||\r\n    (CASE\r\n        WHEN \"ChildFriendliness\" >= 67 THEN 'great for children'\r\n        WHEN \"ChildFriendliness\" >= 34 THEN 'ok for children'\r\n        ELSE 'not suitable for children'\r\n     END))",
                oldStored: true);
        }
    }
}
