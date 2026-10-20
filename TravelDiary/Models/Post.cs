
using System.ComponentModel.DataAnnotations;

namespace TravelDiary.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relacja z użytkownikiem
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        // Relacja ze zdjęciami
        // Jeden post może mieć wiele zdjęć
        public ICollection<PostImage> Images { get; set; }
            = new List<PostImage>();
    }
}
