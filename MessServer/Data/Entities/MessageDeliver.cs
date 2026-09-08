using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessServer.Data.Entities {

    public class MessageDelivery {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid MessageId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public Message Message { get; set; } = null!;

        [Required]
        public Guid RecipientDeviceId { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string EncryptedContent { get; set; } = string.Empty;

        [Required]
        public MessageDeliveryStatus Status { get; set; } = MessageDeliveryStatus.Sent;

        public DateTimeOffset? DeliveredAt { get; set; }

        public DateTimeOffset? ReadAt { get; set; }
    }

    public enum MessageDeliveryStatus {
        Sent = 0,      // Отправлено на сервер
        Delivered = 1, // Доставлено на устройство (получен ACK от клиента)
        Read = 2       // Прочитано пользователем
    }
}