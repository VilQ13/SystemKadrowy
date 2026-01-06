using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Core.Domain;
using SystemKadrowy.Infrastructure.Persistence;

namespace SystemKadrowy.Web.Controllers
{
    public class PracownicyController : Controller
    {
        private readonly KadryDbContext _context;

        public PracownicyController(KadryDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create([Bind("Id,Imie,Nazwisko,PESEL,DataUrodzenia,Email,Telefon,NumerKontaBankowego,ZdjecieSciezka,AdresId,AdresZamieszkania")] Pracownik pracownik)
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

            var pracownik = await _context.Pracownicy.FindAsync(id);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Imie,Nazwisko,PESEL,DataUrodzenia,Email,Telefon,NumerKontaBankowego,ZdjecieSciezka,AdresId,AdresZamieszkania")] Pracownik pracownik)
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
