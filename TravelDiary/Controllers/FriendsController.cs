
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelDiary.Data;
using TravelDiary.Models;

namespace TravelDiary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly TravelDiaryDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FriendsController(
            TravelDiaryDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // POST: api/Friends/request/{receiverId}
        [HttpPost("request/{receiverId}")]
        public async Task<IActionResult> SendRequest(string receiverId)
        {
            var senderId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(senderId))
            {
                return Unauthorized();
            }

            if (senderId == receiverId)
            {
                return BadRequest(new
                {
                    message = "Nie możesz zaprosić samej siebie."
                });
            }

            var receiver = await _userManager.FindByIdAsync(receiverId);

            if (receiver == null)
            {
                return NotFound(new
                {
                    message = "Nie znaleziono użytkownika."
                });
            }

            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(f =>
                    (f.SenderId == senderId &&
                     f.ReceiverId == receiverId) ||
                    (f.SenderId == receiverId &&
                     f.ReceiverId == senderId));

            if (existingRequest != null)
            {
                if (existingRequest.Status == FriendRequestStatus.Pending)
                {
                    return BadRequest(new
                    {
                        message = "Zaproszenie już oczekuje na odpowiedź."
                    });
                }

                if (existingRequest.Status == FriendRequestStatus.Accepted)
                {
                    return BadRequest(new
                    {
                        message = "Jesteście już znajomymi."
                    });
                }

                if (existingRequest.Status == FriendRequestStatus.Rejected)
                {
                    // Ponawiamy zaproszenie, korzystając z istniejącego rekordu.
                    existingRequest.SenderId = senderId;
                    existingRequest.ReceiverId = receiverId;
                    existingRequest.Status = FriendRequestStatus.Pending;
                    existingRequest.CreatedAt = DateTime.UtcNow;
                    existingRequest.RespondedAt = null;

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Zaproszenie zostało wysłane ponownie! 💌",
                        requestId = existingRequest.Id
                    });
                }
            }

            var request = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.FriendRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Zaproszenie zostało wysłane!",
                requestId = request.Id
            });
        }


        // GET: api/Friends/requests/received
        [HttpGet("requests/received")]
        public async Task<IActionResult> GetReceivedRequests()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var requests = await _context.FriendRequests
                .AsNoTracking()
                .Where(f =>
                    f.ReceiverId == userId &&
                    f.Status == FriendRequestStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    id = f.Id,
                    senderId = f.SenderId,
                    senderUserName = f.Sender.UserName,
                    senderDisplayName = f.Sender.DisplayName,
                    senderProfileImageUrl = f.Sender.ProfileImageUrl,
                    createdAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }


        // POST: api/Friends/requests/{requestId}/accept
        [HttpPost("requests/{requestId:int}/accept")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(f =>
                    f.Id == requestId &&
                    f.ReceiverId == userId);

            if (request == null)
            {
                return NotFound(new
                {
                    message = "Nie znaleziono zaproszenia."
                });
            }

            if (request.Status != FriendRequestStatus.Pending)
            {
                return BadRequest(new
                {
                    message = "Na to zaproszenie już odpowiedziano."
                });
            }

            request.Status = FriendRequestStatus.Accepted;
            request.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Zaproszenie zaakceptowane! Jesteście znajomymi.",
                requestId = request.Id,
                senderId = request.SenderId,
                receiverId = request.ReceiverId
            });
        }

        // GET: api/Friends
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var friends = await _context.FriendRequests
                .AsNoTracking()
                .Where(f =>
                    f.Status == FriendRequestStatus.Accepted &&
                    (f.SenderId == userId ||
                     f.ReceiverId == userId))
                .Select(f => new
                {
                    id = f.SenderId == userId
                        ? f.Receiver.Id
                        : f.Sender.Id,

                    userName = f.SenderId == userId
                        ? f.Receiver.UserName
                        : f.Sender.UserName,

                    displayName = f.SenderId == userId
                        ? f.Receiver.DisplayName
                        : f.Sender.DisplayName,

                    profileImageUrl = f.SenderId == userId
                        ? f.Receiver.ProfileImageUrl
                        : f.Sender.ProfileImageUrl
                })
                .OrderBy(f => f.displayName)
                .ToListAsync();

            return Ok(friends);
        }



        // POST: api/Friends/requests/{requestId}/reject
        [HttpPost("requests/{requestId:int}/reject")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(f =>
                    f.Id == requestId &&
                    f.ReceiverId == userId);

            if (request == null)
            {
                return NotFound(new
                {
                    message = "Nie znaleziono zaproszenia."
                });
            }

            if (request.Status != FriendRequestStatus.Pending)
            {
                return BadRequest(new
                {
                    message = "Na to zaproszenie już odpowiedziano."
                });
            }

            request.Status = FriendRequestStatus.Rejected;
            request.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Zaproszenie zostało odrzucone.",
                requestId = request.Id
            });
        }


        // DELETE: api/Friends/{friendId}
        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (userId == friendId)
            {
                return BadRequest(new
                {
                    message = "Nie możesz usunąć samej siebie ze znajomych."
                });
            }

            var friendship = await _context.FriendRequests
                .FirstOrDefaultAsync(f =>
                    f.Status == FriendRequestStatus.Accepted &&
                    (
                        (f.SenderId == userId &&
                         f.ReceiverId == friendId) ||
                        (f.SenderId == friendId &&
                         f.ReceiverId == userId)
                    ));

            if (friendship == null)
            {
                return NotFound(new
                {
                    message = "Nie znaleziono takiej znajomości."
                });
            }

            _context.FriendRequests.Remove(friendship);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Użytkownik został usunięty ze znajomych."
            });
        }

    }
}
