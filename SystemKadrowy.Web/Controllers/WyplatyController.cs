using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Core.Domain;
using SystemKadrowy.Core.Interfaces;
using SystemKadrowy.Infrastructure.Persistence;
using ClosedXML.Excel;
using System.IO;
using SystemKadrowy.Web.Services;

namespace SystemKadrowy.Web.Controllers
{
    [Authorize(Roles = "Admin,Place")]
    public class WyplatyController : Controller
    {
        private readonly KadryDbContext _context;
        private readonly IKalkulatorPlac _kalkulator;
        private readonly AnomalyDetectorService _anomalyService;

        // Konstruktor: Tutaj "prosimy" system o Bazę i Kalkulator
        public WyplatyController(KadryDbContext context, IKalkulatorPlac kalkulator, AnomalyDetectorService anomalyService)
        {
            _context = context;
            _kalkulator = kalkulator;
            _anomalyService = anomalyService;
        }

        // Akcja: Wyświetl listę pracowników, żeby wybrać, komu liczymy wypłatę
        public async Task<IActionResult> Index()
        {
            var pracownicy = await _context.Pracownicy.ToListAsync();
            return View(pracownicy);
        }

        // GET: Wyplaty/Oblicz/5?rok=2025&miesiac=12
        [HttpGet]
        public async Task<IActionResult> Oblicz(int id, int? rok, int? miesiac, decimal premia = 0, decimal potracenie = 0, decimal godziny = 168)
         {
            // 1. Domyślne wartości: Jeśli nie podano daty, przyjmij obecny rok i miesiąc
            int r = rok ?? DateTime.Now.Year;
            int m = miesiac ?? DateTime.Now.Month;

            // Ustawiamy datę wypłaty (np. 10-ty dzień następnego miesiąca lub ostatni dzień bieżącego)
            // Do sprawdzenia ważności umowy przyjmijmy 1. dzień wybranego miesiąca
            DateTime dataPoczatek = new DateTime(r, m, 1);
            DateTime dataKoniec = dataPoczatek.AddMonths(1).AddDays(-1);

            // 2. Pobieramy pracownika z umowami
            var pracownik = await _context.Pracownicy
                .Include(p => p.Umowy)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pracownik == null) return NotFound();



            // 3. LOGIKA CZASU: Szukamy umowy aktywnej w wybranym miesiącu
            // Warunek: Data rozpoczęcia przed lub w trakcie miesiąca ORAZ (Data zakończenia brak LUB po miesiącu)
            var aktywnaUmowa = pracownik.Umowy
                .Where(u => u.DataRozpoczecia.Date <= dataPoczatek.Date) // Zaczęła się wcześniej lub teraz
                .Where(u => u.DataZakonczenia == null || u.DataZakonczenia.Value.Date >= dataPoczatek.Date) // Nie skończyła się
                .FirstOrDefault();

            // Przekazujemy wybraną datę do widoku (żeby wyświetlić na pasku)
            ViewBag.Rok = r;
            ViewBag.Miesiac = m;
            ViewBag.Pracownik = pracownik;

            if (aktywnaUmowa == null)
            {
                // Ważne: Informujemy użytkownika, że w TYM konkretnym miesiącu nie ma umowy
                return Content($"Pracownik {pracownik.Imie} {pracownik.Nazwisko} nie posiada aktywnej umowy w dniu {dataPoczatek:yyyy-MM-dd}.");
            }

            var nieobecnosci = await _context.Nieobecnosci
                .Where(n => n.PracownikId == id)
                // Warunek: Nieobecność zachodzi na ten miesiąc
                .Where(n => n.DataOd <= dataKoniec && n.DataDo >= dataPoczatek)
                .ToListAsync();

            // 4. Przekazujemy listę do kalkulatora
            WynikWyplaty wynik = _kalkulator.Oblicz(aktywnaUmowa, premia, potracenie, godziny, nieobecnosci);

            var ostrzezenieAI = await _anomalyService.SprawdzCzyAnomalia(pracownik.Id, premia);

            if (ostrzezenieAI != null)
            {
                // Przekazujemy ostrzeżenie do widoku za pomocą ViewBag
                ViewBag.AiWarning = ostrzezenieAI;
            }

            ViewBag.WpisanaPremia = premia;
            ViewBag.WpisanePotracenie = potracenie;
            ViewBag.WpisaneGodziny = godziny;
            ViewBag.Rok = r;
            ViewBag.Miesiac = m;
            ViewBag.Pracownik = pracownik;

            return View(wynik);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Zatwierdz(int id, int rok, int miesiac, decimal premia = 0, decimal potracenie = 0, decimal godziny = 168)
        {

            // 1. Pobieramy pracownika i umowę dla danej daty
            DateTime dataPoczatek = new DateTime(rok, miesiac, 1);
            DateTime dataKoniec = dataPoczatek.AddMonths(1).AddDays(-1);


            var pracownik = await _context.Pracownicy
                .Include(p => p.Umowy)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pracownik == null) return NotFound();

            var aktywnaUmowa = pracownik.Umowy
                .Where(u => u.DataRozpoczecia.Date <= dataPoczatek.Date)    
                .Where(u => u.DataZakonczenia == null || u.DataZakonczenia.Value.Date >= dataPoczatek.Date)
                .FirstOrDefault();

            var nieobecnosci = await _context.Nieobecnosci
                .Where(n => n.PracownikId == id)
                // Warunek: Nieobecność zachodzi na ten miesiąc
                .Where(n => n.DataOd <= dataKoniec && n.DataDo >= dataPoczatek)
                .ToListAsync();

            if (aktywnaUmowa == null) return BadRequest("Nie można zatwierdzić: Brak umowy w wybranym miesiącu.");

            // 2. Sprawdzamy DUPLIKATY (czy wypłata za ten miesiąc już jest?)
            bool czyJuzJest = await _context.Wyplaty
                .AnyAsync(w => w.PracownikId == id && w.Rok == rok && w.Miesiac == miesiac);

            if (czyJuzJest)
            {
                // Tutaj przydałoby się wyświetlić błąd, na razie przekierujmy po prostu
                return RedirectToAction("Index");
            }

            // 3. Przeliczamy (Snapshot)
            WynikWyplaty wynik = _kalkulator.Oblicz(aktywnaUmowa, premia, potracenie, godziny, nieobecnosci);

            // 4. Tworzymy rekord historii
            var nowaWyplata = new Wyplata
            {
                PracownikId = id,
                Rok = rok,
                Miesiac = miesiac,
                DataGenerowania = DateTime.Now,

                Brutto = wynik.Brutto,
                Netto = wynik.Netto,
                PremiaBrutto = wynik.PremiaBrutto,
                CalicowiteBrutto = wynik.CalicowiteBrutto,
                PotraceniaKomornicze = wynik.PotraceniaKomornicze,
                DoWyplaty = wynik.DoWyplaty,
                PrzepracowaneGodziny = godziny,
                ZUS_Razem = wynik.ZUS_Razem,
                SkladkaZdrowotna = wynik.SkladkaZdrowotna,
                Podatek = wynik.Podatek,
                KosztyUzyskania = wynik.KosztyUzyskania,

                WynagrodzenieChorobowe = wynik.WynagrodzenieChorobowe,
                PotracenieZaNieobecnosci = wynik.PotracenieZaNieobecnosci,
                IleDniNieobecnosci = wynik.IleDniNieobecnosci,
                IleGodzinNieobecnosci = wynik.IleGodzinNieobecnosci
            };

            // 5. Zapis do SQL
            _context.Wyplaty.Add(nowaWyplata);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index"); // Wracamy do listy pracowników
        }

        // GET: Wyplaty/Details/5 (Tu ID to ID wypłaty, a nie pracownika!)
        public async Task<IActionResult> Details(int id)
        {
            // Pobieramy wypłatę razem z danymi pracownika (żeby wyświetlić Imię i Nazwisko na pasku)
            var wyplata = await _context.Wyplaty
                .Include(w => w.Pracownik)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wyplata == null) return NotFound();

            return View(wyplata);
        }

        // GET: Wyplaty/Drukuj/5
        public async Task<IActionResult> Drukuj(int id)
        {
            var wyplata = await _context.Wyplaty
                .Include(w => w.Pracownik) // Musimy mieć dane osobowe
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wyplata == null) return NotFound();

            return View(wyplata);
        }

        // GET: Wyplaty/EksportExcel
        [Authorize(Roles = "Admin,Place")] // Tylko uprawnieni mogą pobierać
        public async Task<IActionResult> EksportExcel()
        {
            // 1. Pobierz dane z bazy
            var wyplaty = await _context.Wyplaty
                .Include(w => w.Pracownik)
                .OrderByDescending(w => w.DataGenerowania)
                .ToListAsync();

            // 2. Stwórz wirtualny plik Excela
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Lista Płac");

                // 3. Nagłówki tabeli
                worksheet.Cell(1, 1).Value = "Imię i Nazwisko";
                worksheet.Cell(1, 2).Value = "Data Wypłaty";
                worksheet.Cell(1, 3).Value = "Brutto";
                worksheet.Cell(1, 4).Value = "Netto";
                worksheet.Cell(1, 5).Value = "Koszt Pracodawcy";
                worksheet.Cell(1, 6).Value = "Numer Konta";

                // Stylizacja nagłówka (pogrubienie + tło)
                var naglowek = worksheet.Range("A1:F1");
                naglowek.Style.Font.Bold = true;
                naglowek.Style.Fill.BackgroundColor = XLColor.LightGray;

                // 4. Wypełnianie danych (wiersz po wierszu)
                int row = 2;
                foreach (var w in wyplaty)
                {
                    worksheet.Cell(row, 1).Value = w.Pracownik.Imie + " " + w.Pracownik.Nazwisko;
                    worksheet.Cell(row, 2).Value = w.DataGenerowania;

                    worksheet.Cell(row, 3).Value = w.Brutto;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 zł"; // Format walutowy

                    worksheet.Cell(row, 4).Value = w.Netto;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 zł";

                    worksheet.Cell(row, 5).Value = w.DoWyplaty;
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 zł";

                    worksheet.Cell(row, 6).Value = w.Pracownik.NumerKontaBankowego ?? "Brak danych"; // Zabezpieczenie przed nullem

                    row++;
                }

                // 5. Dopasuj szerokość kolumn do treści
                worksheet.Columns().AdjustToContents();

                // 6. Zamień obiekt Excela na strumień bajtów (plik)
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"ListaPlac_{DateTime.Now:yyyyMMdd}.xlsx"
                    );
                }
            }
        }

        public async Task<IActionResult> Historia()
        {
            var wyplaty = await _context.Wyplaty
                .Include(w => w.Pracownik) // Dołączamy pracownika, żeby widzieć nazwisko
                .OrderByDescending(w => w.Rok)
                .ThenByDescending(w => w.Miesiac)
                .ToListAsync();

            return View(wyplaty);
        }


    }
}