
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TravelDiary.Data;
using TravelDiary.DTOs;
using TravelDiary.Models;

namespace TravelDiary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly TravelDiaryDbContext _context;

        public PostsController(TravelDiaryDbContext context)
        {
            _context = context;
        }


        // GET: api/Posts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts()
        {
            var posts = await _context.Posts
                .Select(post => new PostDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    Latitude = post.Latitude,
                    Longitude = post.Longitude,
                    CreatedAt = post.CreatedAt,

                    AuthorName = post.User != null
                        ? post.User.DisplayName
                        : "Nieznany użytkownik",

                    Images = post.Images
                        .OrderBy(image => image.Id)
                        .Select(image => image.ImageUrl)
                        .ToList()
                })
                .ToListAsync();

            return Ok(posts);
        }



        // GET: api/Posts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PostDto>> GetPost(int id)
        {
            var post = await _context.Posts
                .Where(p => p.Id == id)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    CreatedAt = p.CreatedAt,

                    AuthorName = p.User != null
                        ? p.User.DisplayName
                        : "Nieznany użytkownik",

                    Images = p.Images
                        .OrderBy(image => image.Id)
                        .Select(image => image.ImageUrl)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return NotFound();
            }

            return Ok(post);
        }


        // POST: api/Posts
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Post>> CreatePost(CreatePostDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPost),
                new { id = post.Id },
                post);
        }

        // PUT: api/Posts/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, UpdatePostDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != userId)
            {
                return Forbid();
            }

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.Latitude = dto.Latitude;
            post.Longitude = dto.Longitude;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Posts/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != userId)
            {
                return Forbid();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // POST: api/Posts/5/images
        [Authorize]
        [HttpPost("{id}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPostImage(
            int id,
            [FromForm] UploadPostImageDto dto)
        {
            // Sprawdzamy zalogowanego użytkownika
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Szukamy wpisu
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            // Zdjęcie może dodać tylko autor wpisu
            if (post.UserId != userId)
            {
                return Forbid();
            }

            var file = dto.File;

            // Sprawdzamy, czy plik nie jest pusty
            if (file == null || file.Length == 0)
            {
                return BadRequest("Nie wybrano zdjęcia.");
            }

            // Maksymalny rozmiar: 5 MB
            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                return BadRequest("Zdjęcie może mieć maksymalnie 5 MB.");
            }

            // Dozwolone rozszerzenia
            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(
                    "Dozwolone formaty zdjęć: JPG, JPEG, PNG.");
            }

            // Sprawdzamy nagłówek pliku, nie tylko nazwę
            byte[] header = new byte[8];

            using (var stream = file.OpenReadStream())
            {
                var bytesRead = await stream.ReadAsync(
                    header.AsMemory(0, header.Length));

                if (bytesRead < 8)
                {
                    return BadRequest("Nieprawidłowy plik zdjęcia.");
                }
            }

            bool isJpeg =
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;

            bool isPng =
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A;

            bool validFormat =
                (isJpeg && (extension == ".jpg" || extension == ".jpeg")) ||
                (isPng && extension == ".png");

            if (!validFormat)
            {
                return BadRequest("Zawartość pliku nie odpowiada formatowi zdjęcia.");
            }

            // Tworzymy folder na zdjęcia, jeśli nie istnieje
            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "posts");

            Directory.CreateDirectory(folderPath);

            // Generujemy bezpieczną, unikalną nazwę pliku
            var fileName = Guid.NewGuid().ToString("N") + extension;

            var filePath = Path.Combine(folderPath, fileName);

            // Zapisujemy zdjęcie na dysku
            using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            // Zapisujemy adres zdjęcia w bazie danych
            var image = new PostImage
            {
                ImageUrl = "/uploads/posts/" + fileName,
                PostId = post.Id
            };

            try
            {
                _context.PostImages.Add(image);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Usuwamy plik, jeśli zapis w bazie się nie udał
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            return Ok(new
            {
                image.Id,
                image.ImageUrl,
                image.PostId
            });
        }

    }
}
