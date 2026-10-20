using System.ComponentModel.DataAnnotations;

namespace TravelDiary.DTOs
{
    public class CreatePostDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}