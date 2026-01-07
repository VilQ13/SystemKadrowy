using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SystemKadrowy.Web.Filters
{
    public class ZmianaHaslaFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // Sprawdzamy, czy użytkownik jest zalogowany I czy ma naszą "naklejkę"
            if (user.Identity.IsAuthenticated && user.HasClaim(c => c.Type == "WymuszonaZmianaHasla"))
            {
                // Sprawdzamy, gdzie użytkownik próbuje wejść.
                // Jeśli już jest na stronie zmiany hasła (KontoController), to pozwalamy mu tam być.
                // Gdybyśmy tego nie sprawdzili, powstałaby nieskończona pętla przekierowań.
                var controller = context.RouteData.Values["controller"]?.ToString();
                var action = context.RouteData.Values["action"]?.ToString();

                bool czyToStronaZmianyHasla =
                    (controller == "Konto" && action == "ZmienHaslo") ||
                    (controller == "Uzytkownicy" && action == "Logout"); // Pozwólmy też się wylogować

                if (!czyToStronaZmianyHasla)
                {
                    // Jeśli próbuje wejść gdzie indziej -> STOP -> Przekieruj na zmianę hasła
                    context.Result = new RedirectToActionResult("ZmienHaslo", "Konto", null);
                    return;
                }
            }

            // Jeśli warunki nie są spełnione, idź dalej (wykonaj oryginalną akcję)
            await next();
        }
    }
}