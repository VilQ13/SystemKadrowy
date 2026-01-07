using SystemKadrowy.Core.Domain;

namespace SystemKadrowy.Web.Models
{
    public class DashboardViewModel
    {
        // Sekcja KADRY
        public int LiczbaPracownikow { get; set; }
        public int LiczbaAktywnychUmow { get; set; }
        public List<Pracownik> OstatnioZatrudnieni { get; set; } = new List<Pracownik>();

        // Sekcja PŁACE
        public decimal SumaWyplatWtymMiesiacu { get; set; }
        public int LiczbaWygenerowanychWyplat { get; set; }
        public List<Wyplata> OstatnieWyplaty { get; set; } = new List<Wyplata>();
    }
}