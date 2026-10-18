using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    public partial class AddSignalStrength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NetworkType",
                table: "Chargers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rssi",
                table: "Chargers",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetworkType",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "Rssi",
                table: "Chargers");
        }
    }
}
