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
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<KadryDbContext>();

            // Rejestracja serwisów
            builder.Services.AddScoped<IKalkulatorPlac, KalkulatorPlacService>();
            builder.Services.AddScoped<SystemKadrowy.Web.Services.AnomalyDetectorService>();
            builder.Services.AddTransient<SystemKadrowy.Web.Services.DaneTestoweSeeder>();

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<ZmianaHaslaFilter>();
            });

            QuestPDF.Settings.License = LicenseType.Community;

            var app = builder.Build();

            var defaultDateCulture = "pl-PL";
            var ci = new CultureInfo(defaultDateCulture);
            // Upewniamy siê, ¿e liczby maj¹ przecinek, a waluta to z³
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

                try
                {
                    // Migracja Bazy (Tworzenie struktury)
                    var context = services.GetRequiredService<KadryDbContext>();
                    context.Database.Migrate();

                    // Seeder U¿ytkowników i Ról
                    SystemKadrowy.Web.Services.UserSeeder.SeedRolesAndAdminAsync(services).Wait();

                    // Seeder Danych Testowych
                    var dataSeeder = services.GetRequiredService<SystemKadrowy.Web.Services.DaneTestoweSeeder>();
                    dataSeeder.ZainicjujDane().Wait();
                }
                catch (Exception ex)
                {
                    // Logujemy b³¹d, jeœli coœ pójdzie nie tak
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Wyst¹pi³ b³¹d podczas migracji lub inicjalizacji bazy danych.");
                }
            }

            app.Run();
        }
    }
}
