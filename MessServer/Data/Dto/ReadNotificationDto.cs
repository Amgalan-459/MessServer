namespace MessServer.Data.Dto {
    public class ReadNotificationDto {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; } // Кому отправить уведомление
        public Guid RecipientId { get; set; } // Кто прочитал
    }
}
