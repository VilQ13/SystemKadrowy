using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SystemKadrowy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RozbudowaDanychPracownika : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdresId",
                table: "Pracownicy",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumerKontaBankowego",
                table: "Pracownicy",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Pracownicy",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZdjecieSciezka",
                table: "Pracownicy",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KodyPocztowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Miejscowosc = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KodyPocztowe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Adresy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ulica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumerDomu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumerLokalu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KodPocztowyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adresy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adresy_KodyPocztowe_KodPocztowyId",
                        column: x => x.KodPocztowyId,
                        principalTable: "KodyPocztowe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pracownicy_AdresId",
                table: "Pracownicy",
                column: "AdresId");

            migrationBuilder.CreateIndex(
                name: "IX_Adresy_KodPocztowyId",
                table: "Adresy",
                column: "KodPocztowyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pracownicy_Adresy_AdresId",
                table: "Pracownicy",
                column: "AdresId",
                principalTable: "Adresy",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pracownicy_Adresy_AdresId",
                table: "Pracownicy");

            migrationBuilder.DropTable(
                name: "Adresy");

            migrationBuilder.DropTable(
                name: "KodyPocztowe");

            migrationBuilder.DropIndex(
                name: "IX_Pracownicy_AdresId",
                table: "Pracownicy");

            migrationBuilder.DropColumn(
                name: "AdresId",
                table: "Pracownicy");

            migrationBuilder.DropColumn(
                name: "NumerKontaBankowego",
                table: "Pracownicy");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Pracownicy");

            migrationBuilder.DropColumn(
                name: "ZdjecieSciezka",
                table: "Pracownicy");
        }
    }
}
