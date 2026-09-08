namespace MessServer.Data.Dto {
    public class SendMessageDto {
        public Guid RecipientId { get; set; } // Кому
        public List<DeviceMessageDto> DeviceMessages { get; set; } = new(); // Сообщения для каждого устройства
    }

    public class DeviceMessageDto {
        public Guid RecipientDeviceId { get; set; } // Конкретное устройство
        public string EncryptedContent { get; set; } = string.Empty; // Зашифрованный текст для этого устройства
    }
}
