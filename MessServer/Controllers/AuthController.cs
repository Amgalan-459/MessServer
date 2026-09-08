using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MessServer.Data.Context;
using MessServer.Data.Dto;
using MessServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MessServer.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase {
        private readonly MessengerDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController (MessengerDbContext context) {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register ([FromBody] RegisterRequest request) {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username)) {
                return BadRequest("Username already taken");
            }

            User user = new User {
                Id = Guid.NewGuid(),
                Username = request.Username,
                PublicKeyIdentity = request.IdentityPublicKey,
                CreatedAt = DateTimeOffset.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login ([FromBody] LoginRequest request) {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null) return Unauthorized("Invalid username or password");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed) {
                return Unauthorized("Invalid username or password");
            }

            string token = GenerateJwtToken(user);

            return Ok(new AuthResponse {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(AuthOptions.LIFETIME)
            });
        }

        private string GenerateJwtToken (User user) {
            List<Claim> claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            JwtSecurityToken jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(AuthOptions.LIFETIME),
                signingCredentials: new SigningCredentials(
                    AuthOptions.GetSymmetricSecurityKey(),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
