using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MessServer {
    public class AuthOptions {
        public const string ISSUER = "MyAuthServer";
        public const string AUDIENCE = "MyAuthClient";

        const string KEY = "mysupersecret_secretkey!1234567890_very_long_key";

        public const int LIFETIME = 60;

        public static SymmetricSecurityKey GetSymmetricSecurityKey () {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
        }
    }

    public class AuthResponse {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}