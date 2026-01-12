using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SystemKadrowy.Web.Filters
{
    public class ZmianaHaslaFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // Sprawdzamy, czy użytkownik jest zalogowany
            if (user.Identity.IsAuthenticated && user.HasClaim(c => c.Type == "WymuszonaZmianaHasla"))
            {
                var controller = context.RouteData.Values["controller"]?.ToString();
                var action = context.RouteData.Values["action"]?.ToString();

                bool czyToStronaZmianyHasla =
                    (controller == "Konto" && action == "ZmienHaslo") ||
                    (controller == "Uzytkownicy" && action == "Logout");

                if (!czyToStronaZmianyHasla)
                {
                    context.Result = new RedirectToActionResult("ZmienHaslo", "Konto", null);
                    return;
                }
            }

            await next();
        }
    }
}