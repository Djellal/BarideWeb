using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarideWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplateLongValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LongValue",
                table: "Parameters",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Parameters",
                keyColumn: "ParamId",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000001"),
                column: "LongValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "Parameters",
                keyColumn: "ParamId",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000010"),
                column: "LongValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "Parameters",
                keyColumn: "ParamId",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000011"),
                column: "LongValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "Parameters",
                keyColumn: "ParamId",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000012"),
                column: "LongValue",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LongValue",
                table: "Parameters");
        }
    }
}
