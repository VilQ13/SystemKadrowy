using System.ComponentModel.DataAnnotations;

namespace SystemKadrowy.Web.Models
{
    public class RejestracjaViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        public string Haslo { get; set; }

        [Required(ErrorMessage = "Wybór roli jest wymagany")]
        public string Rola { get; set; } // Np. "Kadry", "Place"
    }
}