using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCopilot.Infrastructure.Data.Migrations
{
    public partial class AddTenantIdToAllTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddTenantColumn(migrationBuilder, "parts");
            AddTenantColumn(migrationBuilder, "bom");
            AddTenantColumn(migrationBuilder, "documents");
            AddTenantColumn(migrationBuilder, "embeddings");
            AddTenantColumn(migrationBuilder, "chat_sessions");
            AddTenantColumn(migrationBuilder, "chat_messages");
            AddTenantColumn(migrationBuilder, "feedback");
            AddTenantColumn(migrationBuilder, "part_features");
            AddTenantColumn(migrationBuilder, "digital_twin_state");
            AddTenantColumn(migrationBuilder, "learning_memory");

            migrationBuilder.DropIndex(name: "IX_parts_PartNumber", table: "parts");
            migrationBuilder.CreateIndex(
                name: "IX_parts_tenant_id_PartNumber",
                table: "parts",
                columns: new[] { "tenant_id", "PartNumber" },
                unique: true);

            migrationBuilder.DropIndex(name: "IX_bom_ParentPartId_ChildPartId", table: "bom");
            migrationBuilder.CreateIndex(
                name: "IX_bom_tenant_id_ParentPartId_ChildPartId",
                table: "bom",
                columns: new[] { "tenant_id", "ParentPartId", "ChildPartId" },
                unique: true);

            migrationBuilder.DropIndex(name: "IX_learning_memory_Scenario", table: "learning_memory");
            migrationBuilder.CreateIndex(
                name: "IX_learning_memory_tenant_id_Scenario",
                table: "learning_memory",
                columns: new[] { "tenant_id", "Scenario" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_parts_tenant_id_PartNumber", table: "parts");
            migrationBuilder.CreateIndex(
                name: "IX_parts_PartNumber",
                table: "parts",
                column: "PartNumber",
                unique: true);

            migrationBuilder.DropIndex(name: "IX_bom_tenant_id_ParentPartId_ChildPartId", table: "bom");
            migrationBuilder.CreateIndex(
                name: "IX_bom_ParentPartId_ChildPartId",
                table: "bom",
                columns: new[] { "ParentPartId", "ChildPartId" },
                unique: true);

            migrationBuilder.DropIndex(name: "IX_learning_memory_tenant_id_Scenario", table: "learning_memory");
            migrationBuilder.CreateIndex(
                name: "IX_learning_memory_Scenario",
                table: "learning_memory",
                column: "Scenario",
                unique: true);

            DropTenantColumn(migrationBuilder, "learning_memory");
            DropTenantColumn(migrationBuilder, "digital_twin_state");
            DropTenantColumn(migrationBuilder, "part_features");
            DropTenantColumn(migrationBuilder, "feedback");
            DropTenantColumn(migrationBuilder, "chat_messages");
            DropTenantColumn(migrationBuilder, "chat_sessions");
            DropTenantColumn(migrationBuilder, "embeddings");
            DropTenantColumn(migrationBuilder, "documents");
            DropTenantColumn(migrationBuilder, "bom");
            DropTenantColumn(migrationBuilder, "parts");
        }

        private static void AddTenantColumn(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: tableName,
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "default");
        }

        private static void DropTenantColumn(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: tableName);
        }
    }
}
