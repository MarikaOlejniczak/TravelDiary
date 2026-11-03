
namespace TravelDiary.DTOs
{
    public class PostDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime CreatedAt { get; set; }

        // Identyfikator autora wpisu
        public string AuthorId { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        // Lista adresów zdjęć przypisanych do wpisu
        public List<string> Images { get; set; } = new();
    }
}
