
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelDiary.DTOs;
using TravelDiary.Models;

namespace TravelDiary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/Users/search?query=anna
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query) ||
                query.Trim().Length < 2)
            {
                return BadRequest(new
                {
                    message = "Wpisz co najmniej 2 znaki."
                });
            }

            var searchText = query.Trim();

            if (searchText.Length > 50)
            {
                return BadRequest(new
                {
                    message = "Wyszukiwana nazwa jest za długa."
                });
            }

            var users = await _userManager.Users
                .AsNoTracking()
                .Where(u =>
                    (u.UserName != null &&
                     u.UserName.Contains(searchText)) ||
                    u.DisplayName.Contains(searchText))
                .OrderBy(u => u.UserName)
                .Take(20)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    displayName = u.DisplayName,
                    profileImageUrl = u.ProfileImageUrl
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(string id)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    displayName = u.DisplayName,
                    bio = u.Bio,
                    profileImageUrl = u.ProfileImageUrl,
                    createdAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new
                {
                    message = "Nie znaleziono użytkownika."
                });
            }

            return Ok(user);
        }

        // PUT: api/Users/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            var displayName = dto.DisplayName?.Trim();

            if (string.IsNullOrWhiteSpace(displayName) ||
                displayName.Length < 2 ||
                displayName.Length > 50)
            {
                return BadRequest(new
                {
                    message = "Nazwa musi mieć od 2 do 50 znaków."
                });
            }

            var bio = dto.Bio?.Trim();

            if (bio?.Length > 500)
            {
                return BadRequest(new
                {
                    message = "Opis może mieć maksymalnie 500 znaków."
                });
            }

            user.DisplayName = displayName;
            user.Bio = string.IsNullOrWhiteSpace(bio)
                ? null
                : bio;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new
            {
                message = "Profil został zaktualizowany.",
                id = user.Id,
                userName = user.UserName,
                displayName = user.DisplayName,
                bio = user.Bio,
                profileImageUrl = user.ProfileImageUrl
            });
        }
    }
}
