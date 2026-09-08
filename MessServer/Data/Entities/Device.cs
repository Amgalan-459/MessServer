using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessServer.Data.Entities {
    public class Device {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        public int RegistrationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;

        public string? PushToken { get; set; }

        public virtual ICollection<OneTimePreKey> OneTimePreKeys { get; set; } = new List<OneTimePreKey>();

        public virtual ICollection<SignedPreKey> SignedPreKeys { get; set; } = new List<SignedPreKey>();

        public virtual ICollection<MessageDelivery> Deliveries { get; set; } = new List<MessageDelivery>();
    }
}