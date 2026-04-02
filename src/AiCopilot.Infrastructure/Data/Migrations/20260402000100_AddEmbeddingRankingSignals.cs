using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCopilot.Infrastructure.Data.Migrations
{
    public partial class AddEmbeddingRankingSignals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "feedback_score",
                table: "embeddings",
                type: "double precision",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_used",
                table: "embeddings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "usage_count",
                table: "embeddings",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "feedback_score",
                table: "embeddings");

            migrationBuilder.DropColumn(
                name: "last_used",
                table: "embeddings");

            migrationBuilder.DropColumn(
                name: "usage_count",
                table: "embeddings");
        }
    }
}
