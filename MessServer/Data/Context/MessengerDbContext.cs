using MessServer.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MessServer.Data.Context {
    public class MessengerDbContext : DbContext {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<Chat> Chats { get; set; } = null!;
        public DbSet<ChatParticipant> ChatParticipants { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<MessageDelivery> MessageDeliveries { get; set; } = null!;
        public DbSet<OneTimePreKey> OneTimePreKeys { get; set; } = null!;
        public DbSet<SignedPreKey> SignedPreKeys { get; set; } = null!;
        public DbSet<UserBackup> UserBackups { get; set; }

        public MessengerDbContext (DbContextOptions<MessengerDbContext> options) : base(options) { }

        protected override void OnModelCreating (ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // 1. Уникальный логин
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // 2. Уникальный индекс для участников чата (чтобы нельзя было добавить одного юзера дважды)
            modelBuilder.Entity<ChatParticipant>()
                .HasIndex(cp => new { cp.ChatId, cp.UserId })
                .IsUnique();

            // 3. Быстрый поиск ключей по ID устройства
            modelBuilder.Entity<OneTimePreKey>()
                .HasIndex(k => k.DeviceId);

            modelBuilder.Entity<SignedPreKey>()
                .HasIndex(k => k.DeviceId);

            // 4. Каскадное удаление: если удаляем устройство — удаляются и его ключи
            modelBuilder.Entity<Device>()
                .HasMany(d => d.OneTimePreKeys)
                .WithOne(k => k.Device)
                .HasForeignKey(k => k.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        protected override void OnConfiguring (DbContextOptionsBuilder optionsBuilder) {
            if (!optionsBuilder.IsConfigured) {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=messengerdb;Username=postgres;Password=p@55w0rd;").UseSnakeCaseNamingConvention();

                // СОВЕТ: Установи пакет EFCore.NamingConventions и добавь:
                // .UseSnakeCaseNamingConvention();
                // Это сделает имена таблиц в Postgres маленькими (user_id вместо UserId), что стандарт.
            }
        }
    }
}