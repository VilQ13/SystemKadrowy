using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Core.Domain;
using SystemKadrowy.Infrastructure.Persistence;

namespace SystemKadrowy.Web.Controllers
{
    [Authorize(Roles = "Admin,Kadry")]
    public class NieobecnosciController : Controller
    {
        private readonly KadryDbContext _context;

        public NieobecnosciController(KadryDbContext context)
        {
            _context = context;
        }

        // GET: Nieobecnosci
        public async Task<IActionResult> Index()
        {
            var kadryDbContext = _context.Nieobecnosci.Include(n => n.Pracownik);
            return View(await kadryDbContext.ToListAsync());
        }

        // GET: Nieobecnosci/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nieobecnosc = await _context.Nieobecnosci
                .Include(n => n.Pracownik)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (nieobecnosc == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Zwracamy PartialView -> System wie, żeby NIE dodawać _Layout (menu, stopki)
                // Dzięki temu do okienka trafi sam czysty tekst szczegółów.
                return PartialView(nieobecnosc);
            }

            return View(nieobecnosc);
        }

        // GET: Nieobecnosci/Create
        public IActionResult Create()
        {
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email");
            return View();
        }

        // POST: Nieobecnosci/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PracownikId,DataOd,DataDo,Typ,LiczbaDniRoboczych,LiczbaGodzin")] Nieobecnosc nieobecnosc)
        {
            if (ModelState.IsValid)
            {
                _context.Add(nieobecnosc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email", nieobecnosc.PracownikId);
            return View(nieobecnosc);
        }

        // GET: Nieobecnosci/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nieobecnosc = await _context.Nieobecnosci.FindAsync(id);
            if (nieobecnosc == null)
            {
                return NotFound();
            }
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email", nieobecnosc.PracownikId);
            return View(nieobecnosc);
        }

        // POST: Nieobecnosci/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PracownikId,DataOd,DataDo,Typ,LiczbaDniRoboczych,LiczbaGodzin")] Nieobecnosc nieobecnosc)
        {
            if (id != nieobecnosc.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nieobecnosc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NieobecnoscExists(nieobecnosc.Id))
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
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email", nieobecnosc.PracownikId);
            return View(nieobecnosc);
        }

        // GET: Nieobecnosci/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nieobecnosc = await _context.Nieobecnosci
                .Include(n => n.Pracownik)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (nieobecnosc == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(nieobecnosc);
            }

            return View(nieobecnosc);
        }

        // POST: Nieobecnosci/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nieobecnosc = await _context.Nieobecnosci.FindAsync(id);
            if (nieobecnosc != null)
            {
                _context.Nieobecnosci.Remove(nieobecnosc);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NieobecnoscExists(int id)
        {
            return _context.Nieobecnosci.Any(e => e.Id == id);
        }
    }
}
