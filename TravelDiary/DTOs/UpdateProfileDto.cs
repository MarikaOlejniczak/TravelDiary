
using System.ComponentModel.DataAnnotations;

namespace TravelDiary.DTOs
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Podaj nazwę wyświetlaną.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Nazwa musi mieć od 2 do 50 znaków.")]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(500,
            ErrorMessage = "Opis może mieć maksymalnie 500 znaków.")]
        public string? Bio { get; set; }
    }
}
