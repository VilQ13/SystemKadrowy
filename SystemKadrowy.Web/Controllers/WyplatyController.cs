using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Core.Domain;
using SystemKadrowy.Core.Interfaces;
using SystemKadrowy.Infrastructure.Persistence;

namespace SystemKadrowy.Web.Controllers
{
    [Authorize(Roles = "Admin,Place")]
    public class WyplatyController : Controller
    {
        private readonly KadryDbContext _context;
        private readonly IKalkulatorPlac _kalkulator;

        // Konstruktor: Tutaj "prosimy" system o Bazę i Kalkulator
        public WyplatyController(KadryDbContext context, IKalkulatorPlac kalkulator)
        {
            _context = context;
            _kalkulator = kalkulator;
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

                // Kopiujemy wartości "na sztywno"
                Brutto = wynik.Brutto,
                Netto = wynik.Netto,
                PremiaBrutto = wynik.PremiaBrutto,
                PotraceniaKomornicze = wynik.PotraceniaKomornicze,
                DoWyplaty = wynik.DoWyplaty, // <-- Zapisujemy finalną kwotę
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
    }
}