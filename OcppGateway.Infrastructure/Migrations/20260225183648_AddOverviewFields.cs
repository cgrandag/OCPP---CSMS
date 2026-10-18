using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    public partial class AddOverviewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKwh",
                table: "Transactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "Transactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Chargers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerKwh",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Chargers");
        }
    }
}
