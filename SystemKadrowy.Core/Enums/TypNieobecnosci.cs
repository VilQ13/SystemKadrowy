using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemKadrowy.Core.Enums
{
    public enum TypNieobecnosc
    {
        UrlopWypoczynkowy = 0, // Płatny 100%
        Chorobowe = 1,         // Płatne 80% (uproszczenie: płatne przez pracodawcę)
        UrlopBezplatny = 2,    // Płatne 0%
        NieobecnoscNieusprawiedliwiona = 3 // Płatne 0%
    }
}
