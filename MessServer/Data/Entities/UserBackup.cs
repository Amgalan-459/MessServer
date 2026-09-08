using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessServer.Data.Entities {
    public class UserBackup {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        // Хеш или версия, чтобы клиент понимал, нужно ли скачивать бэкап
        public string BackupHash { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}