namespace TravelDiary.Models
{
    public class PostLike
    {
        public int Id { get; set; }

        // Polubiony wpis
        public int PostId { get; set; }

        public Post Post { get; set; } = null!;

        // Użytkownik, który polubił wpis
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;
    }
}