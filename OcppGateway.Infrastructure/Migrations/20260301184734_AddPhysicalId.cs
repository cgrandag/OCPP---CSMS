using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    public partial class AddPhysicalId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhysicalIdentifier",
                table: "Chargers",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhysicalIdentifier",
                table: "Chargers");
        }
    }
}
