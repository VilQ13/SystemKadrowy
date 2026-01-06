using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SystemKadrowy.Web.Models; // Pamiętaj o tym usingu!

namespace SystemKadrowy.Web.Controllers
{
    [Authorize(Roles = "Admin")] // <--- TYLKO ADMIN MOŻE TU WEJŚĆ
    public class UzytkownicyController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UzytkownicyController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. LISTA UŻYTKOWNIKÓW
        public async Task<IActionResult> Index()
        {
            // Pobieramy wszystkich użytkowników
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // 2. FORMULARZ TWORZENIA (GET)
        public IActionResult Create()
        {
            // Przekazujemy listę ról do listy rozwijanej (Dropdown)
            // SelectList(Źródło, Wartość_Do_Bazy, Wartość_Wyświetlana)
            ViewBag.Role = new SelectList(_roleManager.Roles, "Name", "Name");
            return View();
        }

        // 3. LOGIKA TWORZENIA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RejestracjaViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };

                // A. Tworzymy użytkownika
                var result = await _userManager.CreateAsync(user, model.Haslo);

                if (result.Succeeded)
                {
                    // B. Jeśli się udało, przypisujemy rolę
                    await _userManager.AddToRoleAsync(user, model.Rola);
                    return RedirectToAction(nameof(Index));
                }

                // Jeśli były błędy (np. za słabe hasło), dodaj je do widoku
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Jeśli coś poszło nie tak, wyświetl formularz ponownie (z listą ról)
            ViewBag.Role = new SelectList(_roleManager.Roles, "Name", "Name");
            return View(model);
        }
    }
}