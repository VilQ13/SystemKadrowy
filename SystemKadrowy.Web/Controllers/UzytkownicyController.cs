using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Web.Models;

namespace SystemKadrowy.Web.Controllers
{
    [Authorize(Roles = "Admin")] // Tylko Admin może zarządzać kontami
    public class UzytkownicyController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // <-- NOWOŚĆ: Zarządzanie rolami

        public UzytkownicyController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Uzytkownicy (Lista)
        public async Task<IActionResult> Index()
        {
            var uzytkownicy = await _userManager.Users.ToListAsync();
            // Możemy tu dodać ViewModel, żeby wyświetlić też role na liście, 
            // ale na razie prosta lista wystarczy.
            return View(uzytkownicy);
        }

        // GET: Uzytkownicy/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Pobieramy role tego konkretnego użytkownika
            var userRoles = await _userManager.GetRolesAsync(user);

            // Pobieramy listę wszystkich dostępnych ról w systemie
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EdytujUzytkownikaViewModel
            {
                Id = user.Id,
                Email = user.Email,
                PrzypisaneRole = userRoles,
                DostepneRole = allRoles
            };

            return View(model);
        }

        // POST: Uzytkownicy/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EdytujUzytkownikaViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                // 1. Aktualizacja Emaila/Loginu
                if (user.Email != model.Email)
                {
                    user.Email = model.Email;
                    user.UserName = model.Email; // Zakładamy, że login = email
                    await _userManager.UpdateAsync(user);
                }

                // 2. Aktualizacja Ról (To jest kluczowa część)

                // A. Pobierz aktualne role z bazy
                var obecneRole = await _userManager.GetRolesAsync(user);

                // B. Pobierz role wybrane w formularzu (checkboxy)
                // (W tym prostym przykładzie zakładamy, że model.PrzypisaneRole zawiera zaznaczone)
                // Uwaga: W widoku użyjemy sprytnego triku z nazwami checkboxów.
                // Tutaj musimy odczytać formularz ręcznie lub dostosować model pod checkboxy.
                // Zróbmy prościej: Przekażemy wybrane role przez osobny parametr lub "Request.Form".

                // Dla uproszczenia edukacyjnego:
                // Usuwamy użytkownika ze wszystkich ról i dodajemy do wybranych.
                // W produkcji robi się to bardziej "delta" (tylko różnice), ale tak jest czytelniej:

                await _userManager.RemoveFromRolesAsync(user, obecneRole);

                // Pobieramy zaznaczone role z formularza (ponieważ List<string> w modelu słabo wiąże checkboxy)
                var wybraneRole = Request.Form["WybraneRole"].ToList();

                if (wybraneRole.Any())
                {
                    await _userManager.AddToRolesAsync(user, wybraneRole);
                }

                return RedirectToAction(nameof(Index));
            }

            // Jeśli błąd, przywróć listę ról do widoku
            model.DostepneRole = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // GET: Uzytkownicy/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Uzytkownicy/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Opcjonalnie: Najpierw usuń pracownika powiązanego z tym kontem?
                // Tutaj usuwamy tylko login (dostęp). Dane kadrowe zostają bezpieczne.
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}