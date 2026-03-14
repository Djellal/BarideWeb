using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarideWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transferts",
                columns: table => new
                {
                    TransfertId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTransfert = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    IsViewed = table.Column<bool>(type: "boolean", nullable: false),
                    Cid = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<string>(type: "text", nullable: true),
                    ReceiverId = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transferts", x => x.TransfertId);
                    table.ForeignKey(
                        name: "FK_Transferts_AspNetUsers_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transferts_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transferts_Correspondances_Cid",
                        column: x => x.Cid,
                        principalTable: "Correspondances",
                        principalColumn: "Cid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transferts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transferts_Cid",
                table: "Transferts",
                column: "Cid");

            migrationBuilder.CreateIndex(
                name: "IX_Transferts_ReceiverId",
                table: "Transferts",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferts_SenderId",
                table: "Transferts",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferts_TenantId",
                table: "Transferts",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transferts");
        }
    }
}
