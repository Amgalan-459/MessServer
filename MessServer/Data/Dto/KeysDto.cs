namespace MessServer.Data.Dto {
    public class KeyUploadRequest {
        public int RegistrationId { get; set; }
        public string DeviceName { get; set; } = string.Empty;

        // Главный ключ пользователя
        public string IdentityPublicKey { get; set; } = string.Empty;

        // Подписанный ключ
        public SignedPreKeyDto SignedPreKey { get; set; } = null!;

        // Список одноразовых ключей
        public List<OneTimePreKeyDto> OneTimePreKeys { get; set; } = new();
    }

    public class SignedPreKeyDto {
        public int KeyId { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public class OneTimePreKeyDto {
        public int KeyId { get; set; }
        public string PublicKey { get; set; } = string.Empty;
    }

    public class KeyBundleResponse {
        public Guid UserId { get; set; }
        public string IdentityPublicKey { get; set; } = string.Empty;
        public List<DeviceBundleDto> Devices { get; set; } = new();
    }

    public class DeviceBundleDto {
        public Guid DeviceId { get; set; }
        public int RegistrationId { get; set; }
        public SignedPreKeyDto SignedPreKey { get; set; } = null!;
        public OneTimePreKeyDto? OneTimePreKey { get; set; } // Может быть null, если ключи кончились
    }
}
