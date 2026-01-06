using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemKadrowy.Core.Domain
{
    public class Adres
    {
        public int Id { get; set; }

        public string Ulica { get; set; }
        public string NumerDomu { get; set; }
        public string? NumerLokalu { get; set; } // Może być pusty (dom jednorodzinny)

        // Relacja do Kodu Pocztowego
        public int KodPocztowyId { get; set; }
        public KodPocztowy KodPocztowy { get; set; }
    }
}
