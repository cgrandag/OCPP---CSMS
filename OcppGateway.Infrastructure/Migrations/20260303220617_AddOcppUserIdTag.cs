using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    public partial class AddOcppUserIdTag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdTag",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAuthorized",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TariffId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TariffId",
                table: "Users",
                column: "TariffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tariffs_TariffId",
                table: "Users",
                column: "TariffId",
                principalTable: "Tariffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tariffs_TariffId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TariffId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdTag",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAuthorized",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TariffId",
                table: "Users");
        }
    }
}
