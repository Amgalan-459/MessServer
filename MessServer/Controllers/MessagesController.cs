using MessServer.Data.Context;
using MessServer.Data.Dto;
using MessServer.Data.Entities;
using MessServer.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MessServer.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase {
        private readonly MessengerDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController (MessengerDbContext context, IHubContext<ChatHub> hubContext) {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("sync/{deviceId}")]
        public async Task<IActionResult> GetOfflineMessages (Guid deviceId) {
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            bool device = await _context.Devices
                .AnyAsync(d => d.Id == deviceId && d.UserId == userId);

            if (!device) return Forbid("Device does not belong to user");

            // Ищем все доставки для этого устройства со статусом Sent
            var deliveries = await _context.MessageDeliveries
                .Include(d => d.Message)
                .Where(d => d.RecipientDeviceId == deviceId && d.Status == MessageDeliveryStatus.Sent)
                .OrderBy(d => d.Message.CreatedAt)
                .Select(d => new OfflineMessageDto {
                    MessageId = d.MessageId,
                    DeliveryId = d.Id,
                    SenderId = d.Message.SenderId,
                    EncryptedContent = d.EncryptedContent,
                    CreatedAt = d.Message.CreatedAt
                })
                .ToListAsync();

            return Ok(deliveries);
        }

        // Клиент вызывает это после того, как успешно скачал и сохранил сообщения у себя
        [HttpPost("ack")]
        public async Task<IActionResult> AcknowledgeDelivery ([FromBody] List<Guid> deliveryIds) {
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Находим эти доставки и проверяем, что они адресованы устройствам этого пользователя
            var deliveriesToUpdate = await _context.MessageDeliveries
                .Include(d => d.RecipientDeviceId)
                .Where(d => deliveryIds.Contains(d.Id))
                .ToListAsync();

            foreach (var delivery in deliveriesToUpdate) {
                 var device = await _context.Devices.FindAsync(delivery.RecipientDeviceId);
                if (device?.UserId != userId) continue;

                delivery.Status = MessageDeliveryStatus.Delivered;
                delivery.DeliveredAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("read")]
        public async Task<IActionResult> MarkAsRead ([FromBody] List<Guid> deliveryIds) {
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var deliveries = await _context.MessageDeliveries
                .Include(d => d.Message)
                .Where(d => deliveryIds.Contains(d.Id))
                .ToListAsync();

            List<ReadNotificationDto> notificationsToSend = new List<ReadNotificationDto>();

            foreach (var delivery in deliveries) {
                // Меняем статус только если сообщение было доставлено, но еще не прочитано
                if (delivery.Status < MessageDeliveryStatus.Read) {
                    delivery.Status = MessageDeliveryStatus.Read;
                    delivery.ReadAt = DateTimeOffset.UtcNow;

                    notificationsToSend.Add(new ReadNotificationDto {
                        MessageId = delivery.MessageId,
                        SenderId = delivery.Message.SenderId,
                        RecipientId = userId // Тот, кто прочитал
                    });
                }
            }

            await _context.SaveChangesAsync();

            foreach (var notification in notificationsToSend) {
                string senderPrefix = notification.SenderId.ToString();

                var senderConnections = ChatHub.DeviceConnections
                    .Where(kvp => kvp.Key.StartsWith(senderPrefix))
                    .Select(kvp => kvp.Value);

                foreach (var connectionId in senderConnections) {
                    await _hubContext.Clients.Client(connectionId).SendAsync("MessageRead", new {
                        messageId = notification.MessageId,
                        readerId = notification.RecipientId
                    });
                }
            }

            return Ok();
        }
    }
}
