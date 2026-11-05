 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelDiary.Data;
using TravelDiary.DTOs;
using TravelDiary.Models;

namespace TravelDiary.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Pobieramy identyfikatory zaakceptowanych znajomych.
            // Znajomość działa niezależnie od kierunku zaproszenia.
            var friendIds = await _context.FriendRequests
                .Where(f =>
                    f.Status == FriendRequestStatus.Accepted &&
                    (f.SenderId == userId || f.ReceiverId == userId))
                .Select(f =>
                    f.SenderId == userId
                        ? f.ReceiverId
                        : f.SenderId)
                .ToListAsync();

            // Serwer filtruje wpisy przed wysłaniem ich do przeglądarki.
            var posts = await _context.Posts
                .Where(post =>
                    post.UserId == userId ||
                    post.Visibility == "Public" ||
                    (post.Visibility == "Friends" &&
                     post.UserId != null &&
                     friendIds.Contains(post.UserId)))
                .OrderByDescending(post => post.CreatedAt)
                .Select(post => new PostDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    Latitude = post.Latitude,
                    Longitude = post.Longitude,
                    CreatedAt = post.CreatedAt,

                    AuthorId = post.UserId ?? string.Empty,

                    AuthorName = post.User != null
                        ? post.User.DisplayName
                        : "Nieznany użytkownik",

                    Visibility = post.Visibility,

                    // Zwracamy adresy zabezpieczonego API.
                    Images = post.Images
                        .OrderBy(image => image.Id)
                        .Select(image => "/api/post-images/" + image.Id)
                        .ToList()
                })
                .ToListAsync();

            return Ok(posts);
        }





        // GET: api/Posts/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetUserPosts(
            string userId)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Pobieramy identyfikatory zaakceptowanych znajomych.
            var friendIds = await _context.FriendRequests
                .Where(f =>
                    f.Status == FriendRequestStatus.Accepted &&
                    (f.SenderId == currentUserId ||
                     f.ReceiverId == currentUserId))
                .Select(f =>
                    f.SenderId == currentUserId
                        ? f.ReceiverId
                        : f.SenderId)
                .ToListAsync();

            // Pokazujemy tylko wpisy, do których użytkownik ma dostęp.
            var posts = await _context.Posts
                .Where(post =>
                    post.UserId == userId &&
                    (
                        post.UserId == currentUserId ||
                        post.Visibility == "Public" ||
                        (post.Visibility == "Friends" &&
                         post.UserId != null &&
                         friendIds.Contains(post.UserId))
                    ))
                .OrderByDescending(post => post.CreatedAt)
                .Select(post => new PostDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    Latitude = post.Latitude,
                    Longitude = post.Longitude,
                    CreatedAt = post.CreatedAt,

                    AuthorId = post.UserId ?? string.Empty,

                    AuthorName = post.User != null
                        ? post.User.DisplayName
                        : "Nieznany użytkownik",

                    Visibility = post.Visibility,

                    // Zdjęcia pobieramy przez zabezpieczony kontroler.
                    Images = post.Images
                        .OrderBy(image => image.Id)
                        .Select(image => "/api/post-images/" + image.Id)
                        .ToList()
                })
                .ToListAsync();

            return Ok(posts);
        }




        [HttpGet("{id}")]
        public async Task<ActionResult<PostDto>> GetPost(int id)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Najpierw sprawdzamy, czy użytkownik
            // ma prawo zobaczyć ten wpis.
            var post = await _context.Posts
                .Where(p =>
                    p.Id == id &&
                    (
                        p.UserId == currentUserId ||
                        p.Visibility == "Public" ||
                        (
                            p.Visibility == "Friends" &&
                            p.UserId != null &&
                            _context.FriendRequests.Any(f =>
                                f.Status == FriendRequestStatus.Accepted &&
                                (
                                    (f.SenderId == currentUserId &&
                                     f.ReceiverId == p.UserId)
                                    ||
                                    (f.ReceiverId == currentUserId &&
                                     f.SenderId == p.UserId)
                                ))
                        )
                    ))
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    CreatedAt = p.CreatedAt,

                    AuthorId = p.UserId ?? string.Empty,

                    AuthorName = p.User != null
                        ? p.User.DisplayName
                        : "Nieznany użytkownik",

                    Visibility = p.Visibility,

                    // Zwracamy adresy zabezpieczonego API.
                    Images = p.Images
                        .OrderBy(image => image.Id)
                        .Select(image => "/api/post-images/" + image.Id)
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
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,

                // Zapisujemy widoczność wybraną przez użytkownika.
                Visibility = dto.Visibility
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
        public async Task<IActionResult> UpdatePost(
            int id,
            UpdatePostDto dto)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            // Edytować może tylko autor wpisu.
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
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            // Usuwać może autor wpisu lub administrator.
            if (post.UserId != userId && !User.IsInRole("Admin"))
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
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            // Zdjęcia może dodawać tylko autor wpisu.
            if (post.UserId != userId)
            {
                return Forbid();
            }

            var file = dto.File;

            if (file == null || file.Length == 0)
            {
                return BadRequest("Nie wybrano zdjęcia.");
            }

            const long maxFileSize = 20 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                return BadRequest(
                    "Zdjęcie może mieć maksymalnie 20 MB.");
            }

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[]
            {
        ".jpg",
        ".jpeg",
        ".png"
    };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(
                    "Dozwolone formaty zdjęć: JPG, JPEG, PNG.");
            }

            // Odczytujemy pierwsze 8 bajtów pliku.
            byte[] header = new byte[8];
            int totalBytesRead = 0;

            using (var stream = file.OpenReadStream())
            {
                while (totalBytesRead < header.Length)
                {
                    var bytesRead = await stream.ReadAsync(
                        header.AsMemory(
                            totalBytesRead,
                            header.Length - totalBytesRead));

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytesRead += bytesRead;
                }
            }

            if (totalBytesRead < header.Length)
            {
                return BadRequest(
                    "Nieprawidłowy plik zdjęcia.");
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
                (isJpeg &&
                    (extension == ".jpg" || extension == ".jpeg")) ||
                (isPng && extension == ".png");

            if (!validFormat)
            {
                return BadRequest(
                    "Zawartość pliku nie odpowiada formatowi zdjęcia.");
            }

            // Nowe zdjęcia zapisujemy poza folderem wwwroot.
            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "PrivateUploads",
                "posts");

            Directory.CreateDirectory(folderPath);

            var fileName =
                Guid.NewGuid().ToString("N") + extension;

            var filePath = Path.Combine(
                folderPath,
                fileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var image = new PostImage
            {
                // Zachowujemy nazwę pliku w bazie danych.
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
                // Sprzątamy plik, jeśli zapis w bazie się nie udał.
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            // Klient otrzymuje adres zabezpieczonego API.
            return Ok(new
            {
                image.Id,
                ImageUrl = "/api/post-images/" + image.Id,
                image.PostId
            });
        }

    }
}
