using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelDiary.Data;
using TravelDiary.Models;

namespace TravelDiary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostLikesController : ControllerBase
    {
        private readonly TravelDiaryDbContext _context;

        public PostLikesController(TravelDiaryDbContext context)
        {
            _context = context;
        }

        // POST: api/PostLikes/5
        [HttpPost("{postId:int}")]
        public async Task<IActionResult> LikePost(int postId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Sprawdzamy, czy użytkownik ma dostęp do wpisu.
            var canViewPost = await _context.Posts.AnyAsync(p =>
                p.Id == postId &&
                (
                    p.UserId == userId ||
                    p.Visibility == "Public" ||
                    (
                        p.Visibility == "Friends" &&
                        p.UserId != null &&
                        _context.FriendRequests.Any(f =>
                            f.Status == FriendRequestStatus.Accepted &&
                            (
                                (f.SenderId == userId &&
                                 f.ReceiverId == p.UserId) ||
                                (f.ReceiverId == userId &&
                                 f.SenderId == p.UserId)
                            ))
                    )
                ));

            if (!canViewPost)
            {
                return NotFound();
            }

            // Sprawdzamy, czy użytkownik już polubił wpis.
            var alreadyLiked = await _context.PostLikes.AnyAsync(like =>
                like.PostId == postId &&
                like.UserId == userId);

            if (alreadyLiked)
            {
                return Ok(new { liked = true });
            }

            // Zapisujemy nowe polubienie.
            _context.PostLikes.Add(new PostLike
            {
                PostId = postId,
                UserId = userId
            });

            await _context.SaveChangesAsync();

            return Ok(new { liked = true });
        }

        // DELETE: api/PostLikes/5
        [HttpDelete("{postId:int}")]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Sprawdzamy dostęp do wpisu.
            var canViewPost = await _context.Posts.AnyAsync(p =>
                p.Id == postId &&
                (
                    p.UserId == userId ||
                    p.Visibility == "Public" ||
                    (
                        p.Visibility == "Friends" &&
                        p.UserId != null &&
                        _context.FriendRequests.Any(f =>
                            f.Status == FriendRequestStatus.Accepted &&
                            (
                                (f.SenderId == userId &&
                                 f.ReceiverId == p.UserId) ||
                                (f.ReceiverId == userId &&
                                 f.SenderId == p.UserId)
                            ))
                    )
                ));

            if (!canViewPost)
            {
                return NotFound();
            }

            // Szukamy wyłącznie polubienia tego użytkownika.
            var like = await _context.PostLikes.FirstOrDefaultAsync(l =>
                l.PostId == postId &&
                l.UserId == userId);

            if (like == null)
            {
                return Ok(new { liked = false });
            }

            _context.PostLikes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok(new { liked = false });
        }

        // GET: api/PostLikes/5/users
        [HttpGet("{postId:int}/users")]
        public async Task<IActionResult> GetUsersWhoLiked(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Nie ujawniamy listy polubień wpisów bez dostępu.
            var canViewPost = await _context.Posts.AnyAsync(p =>
                p.Id == postId &&
                (
                    p.UserId == userId ||
                    p.Visibility == "Public" ||
                    (
                        p.Visibility == "Friends" &&
                        p.UserId != null &&
                        _context.FriendRequests.Any(f =>
                            f.Status == FriendRequestStatus.Accepted &&
                            (
                                (f.SenderId == userId &&
                                 f.ReceiverId == p.UserId) ||
                                (f.ReceiverId == userId &&
                                 f.SenderId == p.UserId)
                            ))
                    )
                ));

            if (!canViewPost)
            {
                return NotFound();
            }

            // Zwracamy wyłącznie identyfikator i nazwę użytkownika.
            var users = await _context.PostLikes
                .Where(like => like.PostId == postId)
                .OrderBy(like => like.Id)
                .Select(like => new
                {
                    userId = like.UserId,
                    userName = like.User.UserName,
                    displayName = like.User.DisplayName
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}