using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemKadrowy.Core.Domain;
using SystemKadrowy.Core.Enums;
using SystemKadrowy.Core.Interfaces;

namespace SystemKadrowy.Core.Services
{
    public class KalkulatorPlacService : IKalkulatorPlac
    {
        // Główna metoda
        public WynikWyplaty Oblicz(Umowa umowa, decimal premia = 0, decimal potracenie = 0, decimal godziny = 168)
        {
            // 1. Logika wstępna: Ustalamy, jaka jest kwota bazowa do obliczeń.
            // Nie modyfikujemy obiektu 'umowa', tylko tworzymy nową zmienną.
            decimal bazaBrutto = 0;

            if (umowa.SposobWynagradzania == SposobWynagradzania.StalaMiesieczna)
            {
                // Jeśli stała pensja -> ignorujemy godziny, bierzemy kwotę z umowy
                bazaBrutto = umowa.StawkaBrutto;
            }
            else // SposobWynagradzania.StawkaGodzinowa
            {
                // Jeśli stawka godzinowa -> mnożymy stawkę * godziny
                bazaBrutto = umowa.StawkaBrutto * godziny;
            }

            // 2. Przekazujemy WYLICZONĄ BAZĘ (bazaBrutto) do konkretnych kalkulatorów.
            // Dzięki temu metody prywatne nie muszą wiedzieć, czy to było z godziny czy z etatu.
            switch (umowa.TypUmowy)
            {
                case TypUmowy.UmowaOPrace:
                    return ObliczUoP(umowa, bazaBrutto, premia, potracenie);

                case TypUmowy.UmowaZlecenie:
                    return ObliczZlecenie(umowa, bazaBrutto, premia, potracenie);

                case TypUmowy.B2B_Ryczalt:
                    return ObliczB2B_Ryczalt(umowa, bazaBrutto, premia, potracenie);

                default:
                    return new WynikWyplaty { Brutto = bazaBrutto };
            }
        }

        private WynikWyplaty ObliczUoP(Umowa umowa, decimal wyliczonaPodstawa, decimal premia, decimal potracenie)
        {
            var w = new WynikWyplaty(); 
            w.Brutto = wyliczonaPodstawa;
            w.PremiaBrutto = premia;
            w.PotraceniaKomornicze = potracenie;

            w.CalicowiteBrutto = w.Brutto + w.PremiaBrutto;

            // 1. ZUS (9.76% + 1.5% + 2.45%)
            w.ZUS_Emerytalne = Math.Round(w.CalicowiteBrutto * 0.0976m, 2);
            w.ZUS_Rentowe = Math.Round(w.CalicowiteBrutto * 0.0150m, 2);
            w.ZUS_Chorobowe = Math.Round(w.CalicowiteBrutto * 0.0245m, 2);
            w.ZUS_Razem = w.ZUS_Emerytalne + w.ZUS_Rentowe + w.ZUS_Chorobowe;

            // 2. Zdrowotna (9%)
            decimal podstawaZdr = w.CalicowiteBrutto - w.ZUS_Razem;
            w.SkladkaZdrowotna = Math.Round(podstawaZdr * 0.09m, 2);

            // 3. Podatek (Koszty + PIT 12% - Ulga)
            w.KosztyUzyskania = umowa.CzyKosztyPodwyzszone ? 300m : 250m;

            decimal podstawaPit = w.CalicowiteBrutto - w.ZUS_Razem - w.KosztyUzyskania;
            w.PodstawaOpodatkowania = Math.Round(podstawaPit, 0); // Podstawa do pełnych złotych

            decimal podatekWstepny = w.PodstawaOpodatkowania * 0.12m;
            decimal ulga = umowa.CzyUlgaPodatkowa ? 300m : 0m;

            w.Podatek = podatekWstepny - ulga;
            if (w.Podatek < 0) w.Podatek = 0;
            w.Podatek = Math.Round(w.Podatek, 0); // Zaliczka do pełnych złotych

            // 4. Netto
            w.Netto = w.CalicowiteBrutto - w.ZUS_Razem - w.SkladkaZdrowotna - w.Podatek;

            // FINAŁ: Odejmowanie komornika
            w.DoWyplaty = w.Netto - w.PotraceniaKomornicze;

            return w;
        }

        private WynikWyplaty ObliczZlecenie(Umowa umowa, decimal wyliczonaPodstawa, decimal premia, decimal potracenie)
        {
            var w = new WynikWyplaty();

            w.Brutto = wyliczonaPodstawa;
            w.PremiaBrutto = premia;
            w.PotraceniaKomornicze = potracenie;

            w.CalicowiteBrutto = w.Brutto + w.PremiaBrutto;

            // CASE: Student do 26 lat (Zerowy PIT, Brak ZUS)
            // W uproszczeniu: Brutto = Netto
            if (umowa.CzyStudent)
            {
                w.Netto = w.CalicowiteBrutto; 
                w.DoWyplaty = w.Netto - w.PotraceniaKomornicze;
                return w;
            }

            // 1. ZUS Społeczny
            w.ZUS_Emerytalne = Math.Round(w.CalicowiteBrutto * 0.0976m, 2);
            w.ZUS_Rentowe = Math.Round(w.CalicowiteBrutto * 0.0150m, 2);

            // Chorobowe jest dobrowolne na zleceniu
            if (umowa.CzyDobrowolneChorobowe)
            {
                w.ZUS_Chorobowe = Math.Round(w.CalicowiteBrutto * 0.0245m, 2);
            }

            w.ZUS_Razem = w.ZUS_Emerytalne + w.ZUS_Rentowe + w.ZUS_Chorobowe;

            // 2. Zdrowotna (9%)
            decimal podstawaZdr = w.CalicowiteBrutto - w.ZUS_Razem;
            w.SkladkaZdrowotna = Math.Round(podstawaZdr * 0.09m, 2);

            // 3. Koszty uzyskania przychodu (20%)
            // Liczone od przychodu pomniejszonego o ZUS społeczne
            w.KosztyUzyskania = Math.Round(podstawaZdr * 0.20m, 2);

            // 4. Podatek (12%)
            decimal podstawaPit = podstawaZdr - w.KosztyUzyskania;
            w.PodstawaOpodatkowania = Math.Round(podstawaPit, 0);

            decimal podatekWstepny = w.PodstawaOpodatkowania * 0.12m;

            // Od 2023 zleceniobiorca może złożyć PIT-2 (kwotę wolną)
            decimal ulga = umowa.CzyUlgaPodatkowa ? 300m : 0m;

            w.Podatek = podatekWstepny - ulga;
            if (w.Podatek < 0) w.Podatek = 0;
            w.Podatek = Math.Round(w.Podatek, 0);

            // 5. Netto
            w.Netto = w.CalicowiteBrutto - w.ZUS_Razem - w.SkladkaZdrowotna - w.Podatek;


            // FINAŁ: Odejmowanie komornika
            w.DoWyplaty = w.Netto - w.PotraceniaKomornicze;

            return w;
        }

        // --- 3. B2B RYCZAŁT (Nowość!) ---
        private WynikWyplaty ObliczB2B_Ryczalt(Umowa umowa, decimal wyliczonaPodstawa, decimal premia, decimal potracenie)
        {
            var w = new WynikWyplaty();

            w.Brutto = wyliczonaPodstawa;
            w.PremiaBrutto = premia;
            w.PotraceniaKomornicze = potracenie;

            w.CalicowiteBrutto = w.Brutto + w.PremiaBrutto;

            // Stałe ZUS na rok 2025 (Prognoza "Duży ZUS")
            // W prawdziwym systemie te liczby pobierałbyś z bazy danych parametrów!
            decimal zusSpoleczne = 1641.10m; // Emerytalne+Rentowe+Wypadkowe+FP

            if (umowa.CzyDobrowolneChorobowe)
            {
                w.ZUS_Chorobowe = 122.18m; // Prognoza 2025
            }

            // Przypisujemy do pól wynikowych (B2B płaci ryczałtowo, nie procentowo od brutto)
            w.ZUS_Razem = zusSpoleczne + w.ZUS_Chorobowe;

            // Zdrowotna na Ryczałcie zależy od przychodu rocznego.
            // Uproszczenie: Zakładamy, że miesięczny * 12 daje nam próg.
            decimal przychodRoczny = w.Brutto * 12;

            if (przychodRoczny < 60000)
                w.SkladkaZdrowotna = 460.00m; // Próg 1
            else if (przychodRoczny < 300000)
                w.SkladkaZdrowotna = 770.00m; // Próg 2
            else
                w.SkladkaZdrowotna = 1380.00m; // Próg 3

            // Podstawa Opodatkowania na Ryczałcie
            // Przychód - ZUS Społeczny - 50% Składki Zdrowotnej
            decimal odliczenieZdr = w.SkladkaZdrowotna * 0.50m;

            decimal podstawaOpodatkowania = w.Brutto - w.ZUS_Razem - odliczenieZdr;
            w.PodstawaOpodatkowania = Math.Round(podstawaOpodatkowania, 0);

            // Ryczałt (zakładamy 12% dla branży IT, ale to powinno być konfigurowalne)
            // Tutaj hardkodujemy 12%
            w.Podatek = Math.Round(w.PodstawaOpodatkowania * 0.12m, 0);

            // Na rękę (Dochód netto)
            w.Netto = w.CalicowiteBrutto - w.ZUS_Razem - w.SkladkaZdrowotna - w.Podatek;

            // FINAŁ: Odejmowanie komornika
            w.DoWyplaty = w.Netto - w.PotraceniaKomornicze;

            return w;
        }
    }
}
