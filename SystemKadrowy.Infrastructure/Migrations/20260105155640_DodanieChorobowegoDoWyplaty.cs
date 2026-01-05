using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SystemKadrowy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DodanieChorobowegoDoWyplaty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PotracenieZaNieobecnosci",
                table: "Wyplaty",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WynagrodzenieChorobowe",
                table: "Wyplaty",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PotracenieZaNieobecnosci",
                table: "Wyplaty");

            migrationBuilder.DropColumn(
                name: "WynagrodzenieChorobowe",
                table: "Wyplaty");
        }
    }
}
