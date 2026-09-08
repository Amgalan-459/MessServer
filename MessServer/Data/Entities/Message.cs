using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessServer.Data.Entities {
    public class Message {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ChatId { get; set; }

        [ForeignKey(nameof(ChatId))]
        public Chat Chat { get; set; } = null!;

        [Required]
        public Guid SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; } = null!;

        [Required]
        public Guid SenderDeviceId { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string EncryptedContent { get; set; } = string.Empty;

        public MessageType Type { get; set; } = MessageType.Ciphertext;

        [Required]
        public DateTimeOffset CreatedAt { get; set; }
    }
    public enum MessageType {
        Ciphertext = 0, // Обычное зашифрованное сообщение
        KeyExchange = 1, // Служебное сообщение для обмена ключами
        Attachment = 2   // Зашифрованная ссылка на файл
    }
}
