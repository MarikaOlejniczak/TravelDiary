
using System;
using System.ComponentModel.DataAnnotations;

namespace TravelDiary.Models
{
    public enum FriendRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2
    }

    public class FriendRequest
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        public FriendRequestStatus Status { get; set; }
            = FriendRequestStatus.Pending;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        public ApplicationUser Sender { get; set; } = null!;

        public ApplicationUser Receiver { get; set; } = null!;
    }
}
