
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelDiary.Data;
using TravelDiary.Models;

namespace TravelDiary.Controllers
{
    [ApiController]
    [Route("api/post-images")]
    [Authorize]
    public class PostImagesController : ControllerBase
    {
        private readonly TravelDiaryDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PostImagesController(
            TravelDiaryDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetImage(int id)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var image = await _context.PostImages
                .Include(i => i.Post)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null || image.Post == null)
            {
                return NotFound();
            }

            var post = image.Post;

            var isOwner = post.UserId == currentUserId;
            var isPublic = post.Visibility == "Public";

            var isFriend = false;

            if (!isOwner &&
                !isPublic &&
                post.Visibility == "Friends" &&
                post.UserId != null)
            {
                isFriend = await _context.FriendRequests
                    .AnyAsync(f =>
                        f.Status == FriendRequestStatus.Accepted &&
                        (
                            (f.SenderId == currentUserId &&
                             f.ReceiverId == post.UserId)
                            ||
                            (f.ReceiverId == currentUserId &&
                             f.SenderId == post.UserId)
                        ));
            }

            if (!isOwner && !isPublic && !isFriend)
            {
                // Nie ujawniamy istnienia prywatnego zdjęcia.
                return NotFound();
            }

            // Odczytujemy wyłącznie nazwę pliku.
            // Nie ufamy ścieżce zapisanej w bazie.
            var fileName = Path.GetFileName(
                image.ImageUrl.Replace('\\', '/'));

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(fileName)
                .ToLowerInvariant();

            var contentType = extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => null
            };

            if (contentType == null)
            {
                return NotFound();
            }

            // Docelowo zdjęcia będą przechowywane
            // poza katalogiem wwwroot.
            var filePath = Path.Combine(
                _environment.ContentRootPath,
                "PrivateUploads",
                "posts",
                fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "private, no-store";

            return PhysicalFile(filePath, contentType);
        }
    }
}
