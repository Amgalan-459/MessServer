using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessServer.Data.Entities {
    public class SignedPreKey {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid DeviceId { get; set; }

        [ForeignKey(nameof(DeviceId))]
        public Device Device { get; set; } = null!;

        [Required]
        public int KeyId { get; set; }

        [Required]
        public string PublicKey { get; set; } = string.Empty;

        [Required]
        public string Signature { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}