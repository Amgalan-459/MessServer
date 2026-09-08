using MessServer.Data.Context;
using MessServer.Data.Dto;
using MessServer.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MessServer.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KeysController : ControllerBase {
        private readonly MessengerDbContext _context;

        public KeysController (MessengerDbContext context) {
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadKeys ([FromBody] KeyUploadRequest request) {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            if (string.IsNullOrEmpty(user.PublicKeyIdentity)) {
                user.PublicKeyIdentity = request.IdentityPublicKey;
            }

            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.RegistrationId == request.RegistrationId);

            if (device == null) {
                device = new Device {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RegistrationId = request.RegistrationId,
                    DeviceName = request.DeviceName,
                    LastSeen = DateTimeOffset.UtcNow
                };
                _context.Devices.Add(device);
            }

            SignedPreKey signedKey = new SignedPreKey {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                KeyId = request.SignedPreKey.KeyId,
                PublicKey = request.SignedPreKey.PublicKey,
                Signature = request.SignedPreKey.Signature,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Удаляем старые подписанные ключи устройства, если они были
            var oldSignedKeys = _context.SignedPreKeys.Where(k => k.DeviceId == device.Id);
            _context.SignedPreKeys.RemoveRange(oldSignedKeys);
            _context.SignedPreKeys.Add(signedKey);

            List<OneTimePreKey> oneTimeKeys = request.OneTimePreKeys.Select(k => new OneTimePreKey {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                KeyId = k.KeyId,
                PublicKey = k.PublicKey
            }).ToList();

            // Удаляем старые одноразовые ключи (если пользователь переустановил приложение)
            var oldOneTimeKeys = _context.OneTimePreKeys.Where(k => k.DeviceId == device.Id);
            _context.OneTimePreKeys.RemoveRange(oldOneTimeKeys);

            _context.OneTimePreKeys.AddRange(oneTimeKeys);

            await _context.SaveChangesAsync();

            int keysCount = await _context.OneTimePreKeys
                .CountAsync(k => k.DeviceId == device.Id);
            bool needsMore = keysCount < 20;

            return Ok(new {
                message = "Keys uploaded successfully",
                count = oneTimeKeys.Count,
                needs_more_keys = needsMore
            });
        }

        [HttpGet("bundle/{username}")]
        public async Task<IActionResult> GetBundle (string username) {
            var user = await _context.Users
                .Include(u => u.Devices)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound("User not found");

            var response = new KeyBundleResponse {
                UserId = user.Id,
                IdentityPublicKey = user.PublicKeyIdentity,
                Devices = new List<DeviceBundleDto>()
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try {
                foreach (var device in user.Devices) {
                    // Берем последний подписанный ключ устройства
                    var signedKey = await _context.SignedPreKeys
                        .Where(k => k.DeviceId == device.Id)
                        .OrderByDescending(k => k.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (signedKey == null) continue;

                    // Берем один одноразовый ключ
                    var oneTimeKey = await _context.OneTimePreKeys
                        .Where(k => k.DeviceId == device.Id)
                        .FirstOrDefaultAsync();

                    DeviceBundleDto deviceBundle = new DeviceBundleDto {
                        DeviceId = device.Id,
                        RegistrationId = device.RegistrationId,
                        SignedPreKey = new SignedPreKeyDto {
                            KeyId = signedKey.KeyId,
                            PublicKey = signedKey.PublicKey,
                            Signature = signedKey.Signature
                        }
                    };

                    if (oneTimeKey != null) {
                        deviceBundle.OneTimePreKey = new OneTimePreKeyDto {
                            KeyId = oneTimeKey.KeyId,
                            PublicKey = oneTimeKey.PublicKey
                        };

                        _context.OneTimePreKeys.Remove(oneTimeKey);
                    }

                    response.Devices.Add(deviceBundle);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(response);
            }
            catch (Exception) {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error fetching key bundle");
            }
        }
    }
}
