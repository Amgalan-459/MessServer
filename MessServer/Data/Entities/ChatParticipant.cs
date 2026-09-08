using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessServer.Data.Entities {

    public class ChatParticipant {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ChatId { get; set; }

        [ForeignKey(nameof(ChatId))]
        public Chat Chat { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ParticipantRole Role { get; set; } = ParticipantRole.Member;

        [Required]
        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public enum ParticipantRole {
        Member = 0,
        Admin = 1
    }
}