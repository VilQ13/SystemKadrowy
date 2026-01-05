using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemKadrowy.Core.Enums;

namespace SystemKadrowy.Core.Domain
{
    public class Umowa
    {
        public int Id { get; set; }

        // Klucz obcy - czyja to umowa?
        public int PracownikId { get; set; }
        public Pracownik? Pracownik { get; set; } // Nawigacja dla Entity Framework

        public TypUmowy TypUmowy { get; set; }
        public string Stanowisko { get; set; } = string.Empty;

        // Finanse (Używamy decimal!)
        public decimal StawkaBrutto { get; set; }
        // Czy 5000 to "na miesiąc" czy "na godzinę"?
        public SposobWynagradzania SposobWynagradzania { get; set; }

        // Ważność umowy
        public DateTime DataRozpoczecia { get; set; }
        public DateTime? DataZakonczenia { get; set; } // Nullable - bo umowa może być na czas nieokreślony

        // Konfiguracja podatkowa dla tej konkretnej umowy
        public bool CzyKosztyPodwyzszone { get; set; } = false; // np. dojeżdża do pracy
        public bool CzyUlgaPodatkowa { get; set; } = true; // PIT-2
        public bool CzyStudent { get; set; } = false; // Dla zlecenia
        public bool CzyDobrowolneChorobowe { get; set; } = true; // Dla zlecenia/B2B
    }
}
