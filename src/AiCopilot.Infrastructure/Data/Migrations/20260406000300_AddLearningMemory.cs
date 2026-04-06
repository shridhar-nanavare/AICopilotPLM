using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCopilot.Infrastructure.Data.Migrations
{
    public partial class AddLearningMemory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_memory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<string>(type: "text", nullable: false),
                    Plan = table.Column<string>(type: "jsonb", nullable: false),
                    SuccessRate = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastOutcome = table.Column<string>(type: "text", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_memory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learning_memory_Scenario",
                table: "learning_memory",
                column: "Scenario",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_memory");
        }
    }
}
