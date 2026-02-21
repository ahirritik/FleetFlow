using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FleetFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyOfficerAndAnalystUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$dQVUP4Coir8l9TXGgSwtQ./9yXXMkDW9WquzWfeWD1BlAKmsLOteW");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$H3KVrzeNByOLHO9LqQgWIOV02BHDcI0rQpFwybAVPidzT5TUlCliu");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "safety@fleet.com", "Officer Mike", "$2a$11$Ad6opLafNEgb8lzilm7uOepYuYaCYsy8SkXntnMnBxkDST7seZcEm", "SafetyOfficer" },
                    { 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "analyst@fleet.com", "Emma Analyst", "$2a$11$mzKk8e2gcciW0YWpnCkAAuI0eQkoj.5P4MLB63Bs/ZaOg7QddIrF.", "Analyst" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$bXz48Y.u5A5PgtJCptlDx.awkGP.sTmGArMpQ/hj0LK4ycs/jjNEC");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$RgrklAGuQpp6lxAJZ46aTeibme6DKxRg96R6RS4OBBHdDyUJ6vCo.");
        }
    }
}
