using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    public partial class AddChargerInfoFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConnectorType",
                table: "Chargers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPowerKw",
                table: "Chargers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "Chargers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Chargers",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectorType",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "CurrentPowerKw",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Chargers");
        }
    }
}
