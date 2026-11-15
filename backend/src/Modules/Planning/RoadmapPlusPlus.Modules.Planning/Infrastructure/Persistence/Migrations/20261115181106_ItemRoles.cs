using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class ItemRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_roles",
                schema: "planning",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_roles", x => new { x.item_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_item_roles_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "planning",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_roles_role_id",
                schema: "planning",
                table: "item_roles",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_roles",
                schema: "planning");
        }
    }
}
