using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SystemKadrowy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SzczegolyNieobecnosciNaPasku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IleDniNieobecnosci",
                table: "Wyplaty",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "IleGodzinNieobecnosci",
                table: "Wyplaty",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IleDniNieobecnosci",
                table: "Wyplaty");

            migrationBuilder.DropColumn(
                name: "IleGodzinNieobecnosci",
                table: "Wyplaty");
        }
    }
}
