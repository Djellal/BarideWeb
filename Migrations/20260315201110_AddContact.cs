using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BarideWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.ContactId);
                });

            migrationBuilder.InsertData(
                table: "Parameters",
                columns: new[] { "ParamId", "Description", "Key", "TenantId", "Value" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-000000000010"), "الدقة الافتراضية للماسح الضوئي (DPI): 100, 150, 200, 300, 600", "ScannerDpi", new Guid("b0000000-0000-0000-0000-000000000001"), "200" },
                    { new Guid("c0000000-0000-0000-0000-000000000011"), "وضع اللون الافتراضي للماسح الضوئي: Color, Grayscale", "ScannerPixelMode", new Guid("b0000000-0000-0000-0000-000000000001"), "Grayscale" },
                    { new Guid("c0000000-0000-0000-0000-000000000012"), "صيغة الصورة الافتراضية للماسح الضوئي: JPG, PNG, PDF", "ScannerImageFormat", new Guid("b0000000-0000-0000-0000-000000000001"), "PDF" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DeleteData(
                table: "Parameters",
                keyColumn: "ParamId",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Parameters",
                keyColumn: "ParamId",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "Parameters",
                keyColumn: "ParamId",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000012"));
        }
    }
}
