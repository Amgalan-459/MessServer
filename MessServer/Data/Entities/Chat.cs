using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessServer.Data.Entities {

    public class Chat {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public ChatType Type { get; set; } = ChatType.Private;

        public string? Title { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public enum ChatType {
        Private = 0,
        Group = 1
    }
}