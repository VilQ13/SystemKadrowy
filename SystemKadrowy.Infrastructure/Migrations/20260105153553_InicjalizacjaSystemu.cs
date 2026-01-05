using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SystemKadrowy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InicjalizacjaSystemu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pracownicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Imie = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazwisko = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PESEL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataUrodzenia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pracownicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nieobecnosci",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PracownikId = table.Column<int>(type: "int", nullable: false),
                    DataOd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataDo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Typ = table.Column<int>(type: "int", nullable: false),
                    LiczbaDniRoboczych = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nieobecnosci", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nieobecnosci_Pracownicy_PracownikId",
                        column: x => x.PracownikId,
                        principalTable: "Pracownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Umowy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PracownikId = table.Column<int>(type: "int", nullable: false),
                    TypUmowy = table.Column<int>(type: "int", nullable: false),
                    Stanowisko = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StawkaBrutto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SposobWynagradzania = table.Column<int>(type: "int", nullable: false),
                    DataRozpoczecia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataZakonczenia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CzyKosztyPodwyzszone = table.Column<bool>(type: "bit", nullable: false),
                    CzyUlgaPodatkowa = table.Column<bool>(type: "bit", nullable: false),
                    CzyStudent = table.Column<bool>(type: "bit", nullable: false),
                    CzyDobrowolneChorobowe = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Umowy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Umowy_Pracownicy_PracownikId",
                        column: x => x.PracownikId,
                        principalTable: "Pracownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wyplaty",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PracownikId = table.Column<int>(type: "int", nullable: false),
                    Rok = table.Column<int>(type: "int", nullable: false),
                    Miesiac = table.Column<int>(type: "int", nullable: false),
                    DataGenerowania = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Brutto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Netto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrzepracowaneGodziny = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ZUS_Razem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SkladkaZdrowotna = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Podatek = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KosztyUzyskania = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PremiaBrutto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PotraceniaKomornicze = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DoWyplaty = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wyplaty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wyplaty_Pracownicy_PracownikId",
                        column: x => x.PracownikId,
                        principalTable: "Pracownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nieobecnosci_PracownikId",
                table: "Nieobecnosci",
                column: "PracownikId");

            migrationBuilder.CreateIndex(
                name: "IX_Umowy_PracownikId",
                table: "Umowy",
                column: "PracownikId");

            migrationBuilder.CreateIndex(
                name: "IX_Wyplaty_PracownikId",
                table: "Wyplaty",
                column: "PracownikId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nieobecnosci");

            migrationBuilder.DropTable(
                name: "Umowy");

            migrationBuilder.DropTable(
                name: "Wyplaty");

            migrationBuilder.DropTable(
                name: "Pracownicy");
        }
    }
}
