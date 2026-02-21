using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "$2a$11$Rc9MC2dAQyHXEPmU/GEn8eCR4fMhccriBchy3.xAvAwcx2d2S1XCS", null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "$2a$11$SRr/2YChuFBOJfttz23BwuYnvISc961wXhAx/epRMMjgcpMWdCNDq", null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "$2a$11$Ic.uLquUXzUVCB8XcvgzHO7fncNdm2yl2WN4ADwDLnWsHlnWbdhHq", null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "$2a$11$3xckbFIFfoLwx3Nv/MOR9eGghDErgjk7GxLmLN8P/GLNqeb25wd/W", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Users");

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

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$Ad6opLafNEgb8lzilm7uOepYuYaCYsy8SkXntnMnBxkDST7seZcEm");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                column: "PasswordHash",
                value: "$2a$11$mzKk8e2gcciW0YWpnCkAAuI0eQkoj.5P4MLB63Bs/ZaOg7QddIrF.");
        }
    }
}
