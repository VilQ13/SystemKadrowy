using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Core.Domain;
using SystemKadrowy.Infrastructure.Persistence;
using System.Security.Claims;

namespace SystemKadrowy.Web.Controllers
{
    [Authorize(Roles = "Admin,Kadry")]
    public class PracownicyController : Controller
    {
        private readonly KadryDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<IdentityUser> _userManager;

        public PracownicyController(KadryDbContext context, IWebHostEnvironment hostEnvironment, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
        }

        // GET: Pracownicy
        public async Task<IActionResult> Index()
        {
            return View(await _context.Pracownicy.ToListAsync());
        }

        // GET: Pracownicy/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pracownik = await _context.Pracownicy
                .Include(p => p.Umowy)   // Warto widzieć też umowy
                .Include(p => p.Wyplaty) // <--- DODAJ TO (Ładujemy historię)
                .Include(p => p.AdresZamieszkania)
                .ThenInclude(a => a.KodPocztowy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pracownik == null) return NotFound();

            // Sortujemy wypłaty od najnowszej, żeby na górze mieć ostatni miesiąc
            pracownik.Wyplaty = pracownik.Wyplaty
                .OrderByDescending(w => w.Rok)
                .ThenByDescending(w => w.Miesiac)
                .ToList();

            return View(pracownik);
        }

        // GET: Pracownicy/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Pracownicy/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Imie,Nazwisko,PESEL,DataUrodzenia,Email,Telefon,NumerKontaBankowego,ZdjecieSciezka,AdresId,AdresZamieszkania")] Pracownik pracownik, IFormFile? plikZdjecia)
        {
            if (pracownik.AdresZamieszkania != null && pracownik.AdresZamieszkania.KodPocztowy != null)
            {
                string wpisanyKod = pracownik.AdresZamieszkania.KodPocztowy.Kod;
                string wpisanaMiejscowosc = pracownik.AdresZamieszkania.KodPocztowy.Miejscowosc;

                // Sprawdzamy, czy taki kod już istnieje w bazie
                var istniejacyKod = await _context.KodyPocztowe
                    .FirstOrDefaultAsync(k => k.Kod == wpisanyKod && k.Miejscowosc == wpisanaMiejscowosc);

                if (istniejacyKod != null)
                {
                    // Jeśli istnieje, używamy jego ID i nie tworzymy nowego
                    pracownik.AdresZamieszkania.KodPocztowy = null; // Czyścimy obiekt, żeby EF nie próbował go dodać
                    pracownik.AdresZamieszkania.KodPocztowyId = istniejacyKod.Id;
                }
                // Jeśli nie istnieje (istniejacyKod == null), EF sam go utworzy dzięki relacjom.
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(pracownik.Email))
                {
                    // Sprawdzamy, czy taki email już jest w systemie Identity
                    var istniejacyUser = await _userManager.FindByEmailAsync(pracownik.Email);
                    if (istniejacyUser != null)
                    {
                        ModelState.AddModelError("Email", "Konto z tym adresem e-mail już istnieje w systemie.");
                        return View(pracownik);
                    }

                    // Tworzymy nowy obiekt użytkownika
                    var nowyUser = new IdentityUser
                    {
                        UserName = pracownik.Email, // Loginem będzie email
                        Email = pracownik.Email,
                        EmailConfirmed = true // Od razu potwierdzamy, bo to konto pracownicze zakładane przez Admina
                    };

                    // Ustalamy hasło startowe. 
                    // W prawdziwym projekcie wysłałbyś je mailem lub wymusił zmianę przy pierwszym logowaniu.
                    string hasloStartowe = "Start123!";

                    var result = await _userManager.CreateAsync(nowyUser, hasloStartowe);

                    if (result.Succeeded)
                    {
                        // NOWOŚĆ: Dodajemy "naklejkę" (Claim), że hasło musi być zmienione
                        await _userManager.AddClaimAsync(nowyUser, new Claim("WymuszonaZmianaHasla", "Tak"));
                    }

                    if (!result.Succeeded)
                    {
                        // Jeśli Identity zwróci błędy (np. za słabe hasło), dodajemy je do widoku
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, $"Błąd tworzenia konta: {error.Description}");
                        }
                        return View(pracownik); // Przerywamy, nie tworzymy pracownika
                    }

                    // Opcjonalnie: Możesz tutaj dodać użytkownika do roli, np. "Pracownik"
                    // await _userManager.AddToRoleAsync(nowyUser, "Pracownik");
                }

                // --- LOGIKA ZAPISU ZDJĘCIA ---
                if (plikZdjecia != null)
                {
                    // 1. Gdzie zapisać? (Folder wwwroot/zdjecia)
                    string folderZdjec = Path.Combine(_hostEnvironment.WebRootPath, "zdjecia");

                    // Upewnij się, że folder istnieje
                    if (!Directory.Exists(folderZdjec)) Directory.CreateDirectory(folderZdjec);

                    // 2. Unikalna nazwa pliku (żeby dwa pliki 'profilowe.jpg' się nie nadpisały)
                    // Tworzymy np. "profilowe_GUID.jpg"
                    string unikalnaNazwa = Guid.NewGuid().ToString() + "_" + plikZdjecia.FileName;
                    string sciezkaPliku = Path.Combine(folderZdjec, unikalnaNazwa);

                    // 3. Fizyczny zapis na dysk
                    using (var fileStream = new FileStream(sciezkaPliku, FileMode.Create))
                    {
                        await plikZdjecia.CopyToAsync(fileStream);
                    }

                    // 4. Zapisanie ścieżki w bazie (tylko nazwa pliku)
                    pracownik.ZdjecieSciezka = unikalnaNazwa;
                }
                // -----------------------------

                _context.Add(pracownik);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pracownik);
        }

        // GET: Pracowniks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pracownik = await _context.Pracownicy
                .Include(p => p.AdresZamieszkania)      // Pobierz adres
                .ThenInclude(a => a.KodPocztowy)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pracownik == null)
            {
                return NotFound();
            }
            return View(pracownik);
        }

        // POST: Pracownicy/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Imie,Nazwisko,PESEL,DataUrodzenia,Email,Telefon,NumerKontaBankowego,ZdjecieSciezka,AdresId,AdresZamieszkania")] Pracownik pracownik, IFormFile? plikZdjecia)
        {
            if (id != pracownik.Id)
            {
                return NotFound();
            }

            if (pracownik.AdresZamieszkania != null && pracownik.AdresZamieszkania.KodPocztowy != null)
            {
                string wpisanyKod = pracownik.AdresZamieszkania.KodPocztowy.Kod;
                string wpisanaMiejscowosc = pracownik.AdresZamieszkania.KodPocztowy.Miejscowosc;

                // Sprawdzamy, czy taki kod już istnieje w bazie
                var istniejacyKod = await _context.KodyPocztowe
                    .FirstOrDefaultAsync(k => k.Kod == wpisanyKod && k.Miejscowosc == wpisanaMiejscowosc);

                if (istniejacyKod != null)
                {
                    // Jeśli istnieje, używamy jego ID i nie tworzymy nowego
                    pracownik.AdresZamieszkania.KodPocztowy = null; // Czyścimy obiekt, żeby EF nie próbował go dodać
                    pracownik.AdresZamieszkania.KodPocztowyId = istniejacyKod.Id;
                }
                // Jeśli nie istnieje (istniejacyKod == null), EF sam go utworzy dzięki relacjom.
            }

            if (ModelState.IsValid)
            {
                // --- LOGIKA ZAPISU ZDJĘCIA ---
                if (plikZdjecia != null)
                {
                    // 1. Gdzie zapisać? (Folder wwwroot/zdjecia)
                    string folderZdjec = Path.Combine(_hostEnvironment.WebRootPath, "zdjecia");

                    // Upewnij się, że folder istnieje
                    if (!Directory.Exists(folderZdjec)) Directory.CreateDirectory(folderZdjec);

                    // 2. Unikalna nazwa pliku (żeby dwa pliki 'profilowe.jpg' się nie nadpisały)
                    // Tworzymy np. "profilowe_GUID.jpg"
                    string unikalnaNazwa = Guid.NewGuid().ToString() + "_" + plikZdjecia.FileName;
                    string sciezkaPliku = Path.Combine(folderZdjec, unikalnaNazwa);

                    // 3. Fizyczny zapis na dysk
                    using (var fileStream = new FileStream(sciezkaPliku, FileMode.Create))
                    {
                        await plikZdjecia.CopyToAsync(fileStream);
                    }

                    // 4. Zapisanie ścieżki w bazie (tylko nazwa pliku)
                    pracownik.ZdjecieSciezka = unikalnaNazwa;
                }
                else
                {

                }
                    // -----------------------------

                try
                {
                    _context.Update(pracownik);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PracownikExists(pracownik.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pracownik);
        }

        // GET: Pracownicy/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pracownik = await _context.Pracownicy
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pracownik == null)
            {
                return NotFound();
            }

            return View(pracownik);
        }

        // POST: Pracownicy/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pracownik = await _context.Pracownicy.FindAsync(id);
            if (pracownik != null)
            {
                _context.Pracownicy.Remove(pracownik);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PracownikExists(int id)
        {
            return _context.Pracownicy.Any(e => e.Id == id);
        }
    }
}
