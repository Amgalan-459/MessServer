namespace MessServer.Data.Dto {
    public class RegisterRequest {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string IdentityPublicKey { get; set; } = string.Empty;
    }

    public class LoginRequest {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
