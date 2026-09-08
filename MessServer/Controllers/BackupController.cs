using System.Security.Claims;
using MessServer.Data.Context;
using MessServer.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MessServer.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BackupController : ControllerBase {
        private readonly MessengerDbContext _context;

        public BackupController (MessengerDbContext context) {
            _context = context;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)] //100мб
        public async Task<IActionResult> UploadBackup ([FromBody] byte[] backupData) { //далее переделать это в хранение на диске, а не в бд и сделать потоковую передачу через stream
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var existingBackup = await _context.UserBackups
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (existingBackup != null) {
                existingBackup.Data = backupData;
                existingBackup.CreatedAt = DateTimeOffset.UtcNow;
                // Можно добавить вычисление хеша для проверки целостности
            }
            else {
                UserBackup newBackup = new() {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Data = backupData,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _context.UserBackups.Add(newBackup);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Backup uploaded successfully" });
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadBackup () {
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var backup = await _context.UserBackups
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (backup == null) {
                return NotFound("No backup found for this user");
            }

            return File(backup.Data, "application/octet-stream", "backup.bin");
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetBackupInfo () {
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var backup = await _context.UserBackups
                .Select(b => new { b.UserId, b.CreatedAt, b.Data.Length })
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (backup == null) return NotFound();

            return Ok(backup);
        }
    }
}