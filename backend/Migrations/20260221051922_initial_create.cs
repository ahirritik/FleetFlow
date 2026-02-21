using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class initial_create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$.lkCsCF6P5Gzd8v63aHkq.oD0TdxubWNKcgNkLfoxfoLD38SW9qXq");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$3Ibd0g.DtvpiuuU9/Zk8W.luhpquZjP1mfWe6hxlKvxGxtOg2uWVC");
        }
    }
}
