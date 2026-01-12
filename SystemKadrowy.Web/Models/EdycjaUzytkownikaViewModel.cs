using System.ComponentModel.DataAnnotations;

namespace SystemKadrowy.Web.Models
{
    public class EdytujUzytkownikaViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Adres Email (Login)")]
        public string Email { get; set; }

        // Lista ról, które użytkownik JUŻ posiada
        public IList<string> PrzypisaneRole { get; set; } = new List<string>();

        // Lista WSZYSTKICH ról w systemie
        public List<string> DostepneRole { get; set; } = new List<string>();
    }
}