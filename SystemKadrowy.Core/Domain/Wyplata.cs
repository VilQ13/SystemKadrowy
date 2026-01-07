using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemKadrowy.Core.Domain
{
    public class Wyplata
    {
        public int Id { get; set; }

        // Relacja: Kogo dotyczy wypłata
        public int PracownikId { get; set; }
        public Pracownik? Pracownik { get; set; }

        // Kontekst czasu (za jaki okres)
        public int Rok { get; set; }
        public int Miesiac { get; set; }
        public DateTime DataGenerowania { get; set; } = DateTime.Now;

        // --- WARTOŚCI FINANSOWE (Kopia paska wypłaty) ---
        // Zapisujemy same liczby, żeby historia była "martwa" (niezmienna)
        public decimal Brutto { get; set; }
        public decimal Netto { get; set; }

        public decimal PrzepracowaneGodziny { get; set; }

        // Szczegóły (do raportów)
        public decimal ZUS_Razem { get; set; }
        public decimal SkladkaZdrowotna { get; set; }
        public decimal Podatek { get; set; } // Zaliczka PIT
        public decimal KosztyUzyskania { get; set; }
        public decimal PremiaBrutto { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CalicowiteBrutto { get; set; }
        public decimal WynagrodzenieChorobowe { get; set; } // To jest te 80%
        public decimal PotracenieZaNieobecnosci { get; set; } // To jest to co odejmujemy z podstawy
        public int IleDniNieobecnosci { get; set; }       // Np. 3 dni
        public decimal IleGodzinNieobecnosci { get; set; } // Np. 2.5 godziny
        public decimal PotraceniaKomornicze { get; set; }
        public decimal DoWyplaty { get; set; } // To jest najważniejsza kwota teraz
    }
}