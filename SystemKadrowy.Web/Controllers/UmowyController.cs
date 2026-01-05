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
    public class UmowyController : Controller
    {
        private readonly KadryDbContext _context;

        public UmowyController(KadryDbContext context)
        {
            _context = context;
        }

        // GET: Umowy
        public async Task<IActionResult> Index()
        {
            var kadryDbContext = _context.Umowy.Include(u => u.Pracownik);
            return View(await kadryDbContext.ToListAsync());
        }

        // GET: Umowy/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var umowa = await _context.Umowy
                .Include(u => u.Pracownik)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (umowa == null)
            {
                return NotFound();
            }

            return View(umowa);
        }

        // GET: Umowy/Create
        public IActionResult Create()
        {
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email");
            return View();
        }

        // POST: Umowy/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PracownikId,TypUmowy,SposobWynagradzania,Stanowisko,StawkaBrutto,DataRozpoczecia,DataZakonczenia,CzyKosztyPodwyzszone,CzyUlgaPodatkowa,CzyStudent,CzyDobrowolneChorobowe")] Umowa umowa)
        {
            if (ModelState.IsValid)
            {
                _context.Add(umowa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email", umowa.PracownikId);
            return View(umowa);
        }

        // GET: Umowy/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var umowa = await _context.Umowy.FindAsync(id);
            if (umowa == null)
            {
                return NotFound();
            }
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email", umowa.PracownikId);
            return View(umowa);
        }

        // POST: Umowy/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PracownikId,TypUmowy,SposobWynagradzania,Stanowisko,StawkaBrutto,DataRozpoczecia,DataZakonczenia,CzyKosztyPodwyzszone,CzyUlgaPodatkowa,CzyStudent,CzyDobrowolneChorobowe")] Umowa umowa)
        {
            if (id != umowa.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(umowa);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UmowaExists(umowa.Id))
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
            ViewData["PracownikId"] = new SelectList(_context.Pracownicy, "Id", "Email", umowa.PracownikId);
            return View(umowa);
        }

        // GET: Umowy/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var umowa = await _context.Umowy
                .Include(u => u.Pracownik)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (umowa == null)
            {
                return NotFound();
            }

            return View(umowa);
        }

        // POST: Umowy/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var umowa = await _context.Umowy.FindAsync(id);
            if (umowa != null)
            {
                _context.Umowy.Remove(umowa);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UmowaExists(int id)
        {
            return _context.Umowy.Any(e => e.Id == id);
        }
    }
}
