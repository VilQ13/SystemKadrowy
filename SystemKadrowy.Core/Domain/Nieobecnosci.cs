using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using SystemKadrowy.Core.Enums;

namespace SystemKadrowy.Core.Domain
{
    public class Nieobecnosc
    {
        public int Id { get; set; }

        public int PracownikId { get; set; }
        public Pracownik? Pracownik { get; set; }

        public DateTime DataOd { get; set; }
        public DateTime DataDo { get; set; }

        public TypNieobecnosc Typ { get; set; }

        // Pomocnicze: ile to dni roboczych? (Na razie wpiszemy ręcznie, w przyszłości automat)
        public int LiczbaDniRoboczych { get; set; }
        public decimal LiczbaGodzin { get; set; }
    }
}
