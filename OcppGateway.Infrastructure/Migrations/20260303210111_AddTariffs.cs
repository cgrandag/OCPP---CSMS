using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    public partial class AddTariffs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FlatFee",
                table: "Transactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMinute",
                table: "Transactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TariffId",
                table: "Chargers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tariffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PricePerKwh = table.Column<decimal>(type: "numeric", nullable: false),
                    PricePerMinute = table.Column<decimal>(type: "numeric", nullable: false),
                    FlatFee = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tariffs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_TariffId",
                table: "Chargers",
                column: "TariffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_Tariffs_TariffId",
                table: "Chargers",
                column: "TariffId",
                principalTable: "Tariffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_Tariffs_TariffId",
                table: "Chargers");

            migrationBuilder.DropTable(
                name: "Tariffs");

            migrationBuilder.DropIndex(
                name: "IX_Chargers_TariffId",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "FlatFee",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PricePerMinute",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TariffId",
                table: "Chargers");
        }
    }
}
