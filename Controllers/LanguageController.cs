using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace StockPortal.Controllers
{
    public class LanguageController : Controller
    {
        [HttpGet]
        [HttpPost]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(culture) || (culture != "ar" && culture != "en"))
            {
                culture = "en";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false
                }
            );

            // Also set a simple fallback cookie for JavaScript inspection
            Response.Cookies.Append(
                "stockportal_lang",
                culture,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false
                }
            );

            HttpContext.Session.SetString("Culture", culture);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
