using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SystemKadrowy.Core.Interfaces;
using SystemKadrowy.Core.Services;
using SystemKadrowy.Infrastructure.Persistence;
using SystemKadrowy.Web.Filters;
using QuestPDF.Infrastructure;

namespace SystemKadrowy.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<KadryDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDefaultIdentity<IdentityUser>(options => {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<KadryDbContext>();

            // Rejestracja serwisu obliczeniowego
            builder.Services.AddScoped<IKalkulatorPlac, KalkulatorPlacService>();

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<ZmianaHaslaFilter>();
            });

            QuestPDF.Settings.License = LicenseType.Community;

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
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapRazorPages();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                SystemKadrowy.Web.Services.UserSeeder.SeedRolesAndAdminAsync(services).Wait(); ;
            }

            app.Run();
        }
    }
}
