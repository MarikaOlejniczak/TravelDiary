
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TravelDiary.DTOs
{
    public class UploadPostImageDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
