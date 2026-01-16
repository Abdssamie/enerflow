using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enerflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseSequentialIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Simulations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "ResultJson",
                table: "Simulations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Simulations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "ResultJson",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Simulations");
        }
    }
}
