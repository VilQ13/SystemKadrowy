using Bogus; // To jest nasza biblioteka do fake danych
using Bogus.Extensions.Poland;
using Microsoft.AspNetCore.Identity;
using SystemKadrowy.Core.Domain;
using SystemKadrowy.Core.Enums;
using SystemKadrowy.Infrastructure.Persistence;

namespace SystemKadrowy.Web.Services
{
    public class DaneTestoweSeeder
    {
        private readonly KadryDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DaneTestoweSeeder(KadryDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task ZainicjujDane(int liczbaPracownikow = 50)
        {
            // Jeśli mamy już dużo danych, nie dublujmy ich
            if (_context.Pracownicy.Count() > 10) return;

            // Konfiguracja Bogus - ustawiamy język polski!
            var faker = new Faker("pl");

            for (int i = 0; i < liczbaPracownikow; i++)
            {
                // 1. Generujemy dane osobowe
                string imie = faker.Name.FirstName();
                string nazwisko = faker.Name.LastName();
                string email = faker.Internet.Email(imie, nazwisko);

                // Unikalny PESEL (Bogus ma generator PESELi!)
                string pesel = faker.Person.Pesel();

                // 2. Tworzymy PRACOWNIKA
                var pracownik = new Pracownik
                {
                    Imie = imie,
                    Nazwisko = nazwisko,
                    PESEL = pesel,
                    Email = email,
                    DataUrodzenia = faker.Person.DateOfBirth,
                    Telefon = faker.Phone.PhoneNumber(),
                    NumerKontaBankowego = faker.Finance.Iban(),

                    // Generujemy też adres
                    AdresZamieszkania = new Adres
                    {
                        Ulica = faker.Address.StreetName(),
                        NumerDomu = faker.Address.BuildingNumber(),
                        KodPocztowy = new KodPocztowy
                        {
                            Kod = faker.Address.ZipCode(),
                            Miejscowosc = faker.Address.City()
                        }
                    }
                };

                // 3. Tworzymy KONTO UŻYTKOWNIKA (Identity)
                // To jest ten kluczowy moment, którego nie zrobiłbyś importując CSV
                var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, "Start123!"); // Każdy ma hasło Start123!

                if (result.Succeeded)
                {
                    // Przypiszmy losowo rolę (np. co 10-ty to Kadrowiec)
                    if (i % 10 == 0) await _userManager.AddToRoleAsync(user, "Kadry");
                    else if (i % 15 == 0) await _userManager.AddToRoleAsync(user, "Place");

                    // Wymuszona zmiana hasła (nasz ficzer!)
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("WymuszonaZmianaHasla", "Tak"));

                    // Zapisujemy pracownika do bazy kadr
                    _context.Pracownicy.Add(pracownik);
                    await _context.SaveChangesAsync(); // Musimy zapisać, żeby dostać ID pracownika

                    // 4. Dodajemy UMOWĘ (żeby można było liczyć wypłaty)
                    var umowa = new Umowa
                    {
                        PracownikId = pracownik.Id,
                        DataRozpoczecia = DateTime.Now.AddMonths(-faker.Random.Int(12, 60)), // Pracuje od 1-5 lat
                        Stanowisko = faker.Name.JobTitle(),
                        StawkaBrutto = faker.Random.Decimal(4500, 12000), // Zarobki 4.5k - 12k
                        TypUmowy = TypUmowy.UmowaOPrace,
                        SposobWynagradzania = SposobWynagradzania.StalaMiesieczna
                    };
                    _context.Umowy.Add(umowa);

                    // 5. Generujemy HISTORIĘ WYPŁAT (Dla Twojego AI!)
                    // Wygenerujmy wypłaty za ostatnie 6 miesięcy
                    for (int m = 1; m <= 6; m++)
                    {
                        var dataWyplaty = DateTime.Now.AddMonths(-m);
                        decimal premia = faker.Random.Bool(0.3f) ? faker.Random.Decimal(200, 1000) : 0; // 30% szans na premię

                        // Uproszczona symulacja danych finansowych (tylko żeby były w bazie)
                        decimal brutto = umowa.StawkaBrutto + premia;
                        decimal netto = brutto * 0.7m; // Przybliżenie

                        var wyplata = new Wyplata
                        {
                            PracownikId = pracownik.Id,
                            Rok = dataWyplaty.Year,
                            Miesiac = dataWyplaty.Month,
                            DataGenerowania = dataWyplaty,
                            Brutto = brutto,
                            PremiaBrutto = premia,
                            CalicowiteBrutto = brutto,
                            Netto = netto,
                            DoWyplaty = netto,
                            ZUS_Razem = brutto * 0.1371m,
                            SkladkaZdrowotna = brutto * 0.09m,
                            Podatek = brutto * 0.06m
                        };
                        _context.Wyplaty.Add(wyplata);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}