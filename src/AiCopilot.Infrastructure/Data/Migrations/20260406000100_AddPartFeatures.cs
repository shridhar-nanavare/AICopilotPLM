using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCopilot.Infrastructure.Data.Migrations
{
    public partial class AddPartFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "part_features",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailureRate = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    Lifecycle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_part_features_parts_PartId",
                        column: x => x.PartId,
                        principalTable: "parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_part_features_PartId",
                table: "part_features",
                column: "PartId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "part_features");
        }
    }
}
