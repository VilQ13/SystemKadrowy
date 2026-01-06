using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemKadrowy.Core.Domain;

namespace SystemKadrowy.Core.Interfaces
{
    // Wynik obliczeń (DTO - Data Transfer Object)
    // To jest to samo co WynikWyplaty, o którym mówiliśmy wcześniej
    public class WynikWyplaty
    {
        public decimal Brutto { get; set; }
        public decimal PremiaBrutto { get; set; } // Dodatek (opodatkowany)
        public decimal WynagrodzenieChorobowe { get; set; }
        public decimal PotracenieZaNieobecnosci { get; set; }
        public int IleDniNieobecnosci { get; set; }
        public decimal IleGodzinNieobecnosci { get; set; }
        public decimal CalicowiteBrutto { get; set; } // Podstawa + Premia
        public decimal PotraceniaKomornicze { get; set; } // Odejmowane z ręki
        public decimal DoWyplaty { get; set; } // To co faktycznie idzie przelewem
        public decimal Netto { get; set; }
        public decimal KosztyUzyskania { get; set; }
        public decimal PodstawaOpodatkowania { get; set; }
        public decimal Podatek { get; set; }
        public decimal SkladkaZdrowotna { get; set; }
        public decimal ZUS_Emerytalne { get; set; }
        public decimal ZUS_Rentowe { get; set; }
        public decimal ZUS_Chorobowe { get; set; }
        public decimal ZUS_Razem { get; set; }
    }

    public interface IKalkulatorPlac
    {
        WynikWyplaty Oblicz(
            Umowa umowa, 
            decimal premia = 0, 
            decimal potracenie = 0, 
            decimal godziny = 168, 
            List<Nieobecnosc>? nieobecnosci = null
            );
    }
}
