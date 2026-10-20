
namespace TravelDiary.Models
{
    public class PostImage
    {
        public int Id { get; set; }

        // Ścieżka do zapisanego zdjęcia
        public string ImageUrl { get; set; } = string.Empty;

        // Identyfikator wpisu, do którego należy zdjęcie
        public int PostId { get; set; }

        // Relacja ze wpisem
        public Post Post { get; set; } = null!;
    }
}
