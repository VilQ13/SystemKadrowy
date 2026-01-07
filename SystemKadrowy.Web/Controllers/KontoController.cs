using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SystemKadrowy.Web.Controllers
{
    [Authorize] // Tylko zalogowani
    public class KontoController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public KontoController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult ZmienHaslo()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZmienHaslo(string stareHaslo, string noweHaslo, string powtorzHaslo)
        {
            if (noweHaslo != powtorzHaslo)
            {
                ModelState.AddModelError("", "Nowe hasła muszą być identyczne.");
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Home");

            // 1. Próba zmiany hasła
            var result = await _userManager.ChangePasswordAsync(user, stareHaslo, noweHaslo);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View();
            }

            // 2. Sukces! Odklejamy "naklejkę" (usuwamy Claim)
            var claim = (await _userManager.GetClaimsAsync(user))
                        .FirstOrDefault(c => c.Type == "WymuszonaZmianaHasla");

            if (claim != null)
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            // 3. WAŻNE: Po zmianie hasła system wylogowuje usera (zmienia się SecurityStamp).
            // Musimy go zalogować ponownie automatycznie, żeby nie wyrzuciło go z systemu.
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction("Index", "Home");
        }
    }
}