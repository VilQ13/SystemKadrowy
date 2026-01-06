using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemKadrowy.Core.Domain
{
    public class KodPocztowy
    {
        public int Id { get; set; }
        public string Kod { get; set; }        // np. "00-001"
        public string Miejscowosc { get; set; } // np. "Warszawa"

        // Relacja: Jeden kod może być użyty w wielu adresach
        public List<Adres> Adresy { get; set; } = new List<Adres>();
    }
}
