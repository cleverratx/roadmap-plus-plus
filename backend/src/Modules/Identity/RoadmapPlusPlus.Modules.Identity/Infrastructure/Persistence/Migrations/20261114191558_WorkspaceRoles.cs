using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class WorkspaceRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "roles_enabled",
                schema: "identity",
                table: "workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.UniqueConstraint("ak_roles_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.ForeignKey(
                        name: "fk_roles_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "identity",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member_roles",
                schema: "identity",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_roles", x => new { x.workspace_id, x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_member_roles_members_workspace_id_user_id",
                        columns: x => new { x.workspace_id, x.user_id },
                        principalSchema: "identity",
                        principalTable: "members",
                        principalColumns: new[] { "workspace_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_member_roles_roles_workspace_id_role_id",
                        columns: x => new { x.workspace_id, x.role_id },
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_member_roles_workspace_id_role_id",
                schema: "identity",
                table: "member_roles",
                columns: new[] { "workspace_id", "role_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "member_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "roles_enabled",
                schema: "identity",
                table: "workspaces");
        }
    }
}
