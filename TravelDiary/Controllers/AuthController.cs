
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TravelDiary.DTOs;
using TravelDiary.Models;

namespace TravelDiary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                DisplayName = dto.DisplayName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(
                user,
                dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new
            {
                message = "Użytkownik został utworzony."
            });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Nieprawidłowy email lub hasło."
                });
            }

            var passwordValid = await _userManager.CheckPasswordAsync(
                user,
                dto.Password);

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    message = "Nieprawidłowy email lub hasło."
                });
            }

            // Dane użytkownika zapisane w tokenie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };

            // Klucz JWT - na razie pozostawiamy
            // zgodny z istniejącą konfiguracją
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    "TravelDiarySuperSecretKey123456789"));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(2);

            var token = new JwtSecurityToken(
                issuer: "TravelDiary",
                audience: "TravelDiary",
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler()
                .WriteToken(token);

            // NOWOŚĆ: zapisujemy JWT w ciasteczku HttpOnly
            Response.Cookies.Append(
                "TravelDiaryAuth",
                tokenString,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = expiresAt,
                    Path = "/"
                });

            // Zachowujemy token w odpowiedzi,
            // żeby nie zepsuć dotychczasowych testów
            return Ok(new
            {
                message = "Logowanie zakończone pomyślnie.",
                token = tokenString,
                userId = user.Id,
                userName = user.UserName,
                displayName = user.DisplayName
            });
        }


        // GET: api/Auth/me
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Pobieramy identyfikator użytkownika z JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Szukamy użytkownika w bazie danych
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            // Zwracamy dane użytkownika, ale nigdy hasło
            return Ok(new
            {
                id = user.Id,
                userName = user.UserName,
                displayName = user.DisplayName,
                email = user.Email
            });
        }


        // POST: api/Auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Usuwamy ciasteczko z przeglądarki
            Response.Cookies.Delete(
                "TravelDiaryAuth",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            return Ok(new
            {
                message = "Wylogowano pomyślnie."
            });
        }


    }
}
