using MessServer.Data.Context;
using MessServer.Data.Dto;
using MessServer.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MessServer.Hubs {
    [Authorize]
    public class ChatHub : Hub {
        private readonly MessengerDbContext _context;

        // Статический словарь для отслеживания: какой DeviceId на каком ConnectionId сидит
        // лучше использовать Redis
        public static readonly Dictionary<string, string> DeviceConnections = new();

        public ChatHub (MessengerDbContext context) {
            _context = context;
        }

        // Вызывается, когда клиент подключается
        public override async Task OnConnectedAsync () {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            // Клиент должен прислать свой DeviceId в заголовке или Query String
            var deviceId = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(deviceId)) {
                string key = $"{userId}_{deviceId}";
                DeviceConnections[key] = Context.ConnectionId;
            }

            await base.OnConnectedAsync();
        }

        public async Task SendEncryptedMessage (SendMessageDto request) {
            Guid senderId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

            string senderDeviceIdStr = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();

            var senderDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == Guid.Parse(senderDeviceIdStr));

            Message message = new Message {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                CreatedAt = DateTimeOffset.UtcNow
                // ChatId можно добавить, если вы его ведете
            };
            _context.Messages.Add(message);

            foreach (var devMsg in request.DeviceMessages) {
                MessageDelivery delivery = new() {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    RecipientDeviceId = devMsg.RecipientDeviceId,
                    EncryptedContent = devMsg.EncryptedContent,
                    Status = MessageDeliveryStatus.Sent
                };
                _context.MessageDeliveries.Add(delivery);

                // Пытаемся найти, онлайн ли это устройство
                var recipientKey = $"{request.RecipientId}_{devMsg.RecipientDeviceId}";
                if (DeviceConnections.TryGetValue(recipientKey, out var connectionId)) {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", new {
                        MessageId = message.Id,
                        SenderId = senderId,
                        SenderRegistrationId = senderDevice?.RegistrationId ?? 0,
                        EncryptedContent = devMsg.EncryptedContent,
                        CreatedAt = message.CreatedAt
                    });

                    delivery.Status = MessageDeliveryStatus.Delivered;
                    delivery.DeliveredAt = DateTimeOffset.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        public override Task OnDisconnectedAsync (Exception? exception) {
            // Убираем соединение из словаря при отключении
            var connectionId = Context.ConnectionId;
            var item = DeviceConnections.FirstOrDefault(kvp => kvp.Value == connectionId);
            if (!item.Equals(default(KeyValuePair<string, string>))) {
                DeviceConnections.Remove(item.Key);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendReadConfirmation (ReadNotificationDto notification) {
            string senderPrefix = notification.SenderId.ToString();

            var senderConnections = DeviceConnections
                .Where(kvp => kvp.Key.StartsWith(senderPrefix))
                .Select(kvp => kvp.Value);

            foreach (string? connectionId in senderConnections) {
                await Clients.Client(connectionId).SendAsync("MessageRead", new {
                    notification.MessageId,
                    notification.RecipientId
                });
            }
        }
    }
}