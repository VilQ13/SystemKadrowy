using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SystemKadrowy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DodanieCalkowitegoBrutta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CalicowiteBrutto",
                table: "Wyplaty",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalicowiteBrutto",
                table: "Wyplaty");

        }
    }
}
