using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    public partial class AddChargerName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Chargers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Chargers");
        }
    }
}
