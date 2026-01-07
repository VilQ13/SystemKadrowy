using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using SystemKadrowy.Infrastructure.Persistence;
using SystemKadrowy.Web.Models;

namespace SystemKadrowy.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly KadryDbContext _context;

        public HomeController(ILogger<HomeController> logger, KadryDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Tworzymy pusty model
            var model = new DashboardViewModel();

            // Sprawdzamy, czy u¿ytkownik jest zalogowany
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // --- LOGIKA DLA KADR (i Admina) ---
                if (User.IsInRole("Kadry") || User.IsInRole("Admin"))
                {
                    model.LiczbaPracownikow = await _context.Pracownicy.CountAsync();

                    // Liczymy aktywne umowy (uproszczony warunek: data koñca jest w przysz³oœci lub null)
                    model.LiczbaAktywnychUmow = await _context.Umowy
                        .CountAsync(u => u.DataZakonczenia == null || u.DataZakonczenia >= DateTime.Now);

                    // Pobieramy 5 najnowszych pracowników (wg ID malej¹co)
                    model.OstatnioZatrudnieni = await _context.Pracownicy
                        .OrderByDescending(p => p.Id)
                        .Take(5)
                        .ToListAsync();
                }

                // --- LOGIKA DLA P£AC (i Admina) ---
                if (User.IsInRole("Place") || User.IsInRole("Admin"))
                {
                    var teraz = DateTime.Now;

                    // Suma wyp³at w bie¿¹cym miesi¹cu
                    model.SumaWyplatWtymMiesiacu = await _context.Wyplaty
                        .Where(w => w.Rok == teraz.Year && w.Miesiac == teraz.Month)
                        .SumAsync(w => w.DoWyplaty);

                    model.LiczbaWygenerowanychWyplat = await _context.Wyplaty
                        .Where(w => w.Rok == teraz.Year && w.Miesiac == teraz.Month)
                        .CountAsync();

                    // 5 ostatnich operacji p³acowych
                    model.OstatnieWyplaty = await _context.Wyplaty
                        .Include(w => w.Pracownik) // Do³¹czamy pracownika, ¿eby wyœwietliæ nazwisko
                        .OrderByDescending(w => w.DataGenerowania)
                        .Take(5)
                        .ToListAsync();
                }
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
