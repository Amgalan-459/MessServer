namespace MessServer.Data.Dto {
    public class OfflineMessageDto {
        public Guid MessageId { get; set; }
        public Guid DeliveryId { get; set; } // ID записи о доставке
        public Guid SenderId { get; set; }
        public string EncryptedContent { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
