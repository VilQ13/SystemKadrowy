using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SystemKadrowy.Core.Interfaces;
using SystemKadrowy.Core.Services;
using SystemKadrowy.Infrastructure.Persistence;

namespace SystemKadrowy.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<KadryDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Rejestracja serwisu obliczeniowego
            builder.Services.AddScoped<IKalkulatorPlac, KalkulatorPlacService>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            var defaultDateCulture = "pl-PL";
            var ci = new CultureInfo(defaultDateCulture);
            // Opcjonalnie: Upewniamy siê, ¿e liczby maj¹ przecinek, a waluta to z³
            ci.NumberFormat.NumberDecimalSeparator = ",";
            ci.NumberFormat.CurrencyDecimalSeparator = ",";

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(ci),
                SupportedCultures = new List<CultureInfo> { ci },
                SupportedUICultures = new List<CultureInfo> { ci }
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
