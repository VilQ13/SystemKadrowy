using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemKadrowy.Core.Domain
{
    public class Pracownik
    {
        public int Id { get; set; } // Unikalne ID z bazy danych
        public string Imie { get; set; } = string.Empty;
        public string Nazwisko { get; set; } = string.Empty;
        public string PESEL { get; set; } = string.Empty;
        public DateTime DataUrodzenia { get; set; }
        public string Email { get; set; } = string.Empty;

        // Relacja: Jeden pracownik ma wiele umów (historia zatrudnienia)
        public List<Umowa> Umowy { get; set; } = new List<Umowa>();
        public List<Wyplata> Wyplaty { get; set; } = new List<Wyplata>();
        public List<Nieobecnosc> Nieobecnosci { get; set; } = new List<Nieobecnosc>();
    }
}
