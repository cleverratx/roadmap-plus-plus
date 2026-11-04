using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class InitialPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "planning");

            migrationBuilder.CreateTable(
                name: "items",
                schema: "planning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description_md = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                    table.CheckConstraint("ck_items_dates_order", "end_date >= start_date");
                    table.CheckConstraint("ck_items_dates_together", "(start_date IS NULL) = (end_date IS NULL)");
                    table.ForeignKey(
                        name: "fk_items_items_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "planning",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_items_parent_id",
                schema: "planning",
                table: "items",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_workspace_id_start_date_end_date",
                schema: "planning",
                table: "items",
                columns: new[] { "workspace_id", "start_date", "end_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "items",
                schema: "planning");
        }
    }
}
