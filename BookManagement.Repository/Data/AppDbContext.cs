using System;
using System.Threading;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Repository.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartBookDetail> CartBookDetails { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Delivery> Deliveries { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<ReturnRequest> ReturnRequests { get; set; } = null!;
        public DbSet<TransactionHistory> TransactionHistories { get; set; } = null!;
        public DbSet<Feedback> Feedbacks { get; set; } = null!;
        public DbSet<Response> Responses { get; set; } = null!;
        public DbSet<Chat> Chats { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<IAuditableEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. User
            // ==========================================
            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(u => u.Id);
                builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
                builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
                builder.HasIndex(u => u.Email).IsUnique();
                builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                builder.Property(u => u.FullName).HasMaxLength(100);
                builder.Property(u => u.Phone).HasMaxLength(20);
                builder.Property(u => u.Address).HasMaxLength(255);
                builder.Property(u => u.QrImageUrl).HasMaxLength(255);

                builder.Property(u => u.Role)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(UserRole.CUSTOMER);

                builder.Property(u => u.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(UserStatus.ACTIVE);
            });

            // ==========================================
            // 2. Shop (Quan hệ 1-1 với User)
            // ==========================================
            modelBuilder.Entity<Shop>(builder =>
            {
                builder.HasKey(s => s.Id);
                builder.Property(s => s.ShopName).IsRequired().HasMaxLength(100);

                builder.Property(s => s.Condition)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(ShopCondition.OPEN);

                builder.HasIndex(s => s.UserId).IsUnique();

                builder.HasOne(s => s.User)
                    .WithOne(u => u.Shop)
                    .HasForeignKey<Shop>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 3. Category
            // ==========================================
            modelBuilder.Entity<Category>(builder =>
            {
                builder.HasKey(c => c.Id);
                builder.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
                builder.Property(c => c.Status).HasDefaultValue(true);
            });

            // ==========================================
            // 4. Book
            // ==========================================
            modelBuilder.Entity<Book>(builder =>
            {
                builder.HasKey(b => b.Id);
                builder.Property(b => b.Title).IsRequired().HasMaxLength(255);
                builder.Property(b => b.Isbn).HasMaxLength(20);
                builder.HasIndex(b => b.Isbn).IsUnique().HasFilter("[Isbn] IS NOT NULL");
                builder.Property(b => b.Author).HasMaxLength(150);
                builder.Property(b => b.Publisher).HasMaxLength(150);
                builder.Property(b => b.Price).HasPrecision(10, 2);
                builder.Property(b => b.ImageUrl).HasMaxLength(255);

                builder.Property(b => b.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(BookStatus.ACTIVE);

                builder.HasOne(b => b.Shop)
                    .WithMany(s => s.Books)
                    .HasForeignKey(b => b.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(b => b.Category)
                    .WithMany(c => c.Books)
                    .HasForeignKey(b => b.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 5. Cart (Quan hệ 1-1 với User)
            // ==========================================
            modelBuilder.Entity<Cart>(builder =>
            {
                builder.HasKey(c => c.Id);
                builder.HasIndex(c => c.UserId).IsUnique();

                builder.HasOne(c => c.User)
                    .WithOne(u => u.Cart)
                    .HasForeignKey<Cart>(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 6. CartBookDetail
            // ==========================================
            modelBuilder.Entity<CartBookDetail>(builder =>
            {
                builder.HasKey(cbd => cbd.Id);
                builder.Property(cbd => cbd.UnitPrice).HasPrecision(10, 2);

                builder.HasOne(cbd => cbd.Cart)
                    .WithMany(c => c.CartBookDetails)
                    .HasForeignKey(cbd => cbd.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(cbd => cbd.Book)
                    .WithMany(b => b.CartBookDetails)
                    .HasForeignKey(cbd => cbd.BookId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 7. Order
            // ==========================================
            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(o => o.Id);
                builder.Property(o => o.TotalAmount).HasPrecision(12, 2);
                builder.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(255);
                builder.Property(o => o.Weight).HasPrecision(8, 2);
                builder.Property(o => o.Note).HasMaxLength(255);

                builder.Property(o => o.OrderStatus)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(OrderStatus.PENDING);

                builder.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 8. OrderDetail
            // ==========================================
            modelBuilder.Entity<OrderDetail>(builder =>
            {
                builder.HasKey(od => od.Id);
                builder.Property(od => od.UnitPrice).HasPrecision(10, 2);

                builder.Property(od => od.ReturnStatus)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(ReturnStatus.NONE);

                builder.HasOne(od => od.Order)
                    .WithMany(o => o.OrderDetails)
                    .HasForeignKey(od => od.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(od => od.Book)
                    .WithMany(b => b.OrderDetails)
                    .HasForeignKey(od => od.BookId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 9. Delivery
            // ==========================================
            modelBuilder.Entity<Delivery>(builder =>
            {
                builder.HasKey(d => d.Id);
                builder.Property(d => d.TrackingNumber).HasMaxLength(50);
                builder.Property(d => d.CarrierName).HasMaxLength(100);
                builder.Property(d => d.ShipFee).HasPrecision(10, 2);

                builder.Property(d => d.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(DeliveryStatus.PENDING);

                builder.HasOne(d => d.Order)
                    .WithMany(o => o.Deliveries)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 10. ReturnRequest (Quan hệ 1-1 với OrderDetail)
            // ==========================================
            modelBuilder.Entity<ReturnRequest>(builder =>
            {
                builder.HasKey(rr => rr.Id);
                builder.Property(rr => rr.ImageUrl).HasMaxLength(255);
                builder.Property(rr => rr.RefundAmount).HasPrecision(12, 2);

                builder.Property(rr => rr.ReasonType)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                builder.Property(rr => rr.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(ReturnRequestStatus.PENDING);

                builder.HasIndex(rr => rr.OrderDetailId).IsUnique();

                builder.HasOne(rr => rr.OrderDetail)
                    .WithOne(od => od.ReturnRequest)
                    .HasForeignKey<ReturnRequest>(rr => rr.OrderDetailId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 11. Payment
            // ==========================================
            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(p => p.Id);
                builder.Property(p => p.Amount).HasPrecision(12, 2);

                builder.Property(p => p.PaymentType)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(PaymentType.PAYMENT);

                builder.Property(p => p.Method)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                builder.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(PaymentStatus.PENDING);

                builder.HasOne(p => p.Order)
                    .WithMany(o => o.Payments)
                    .HasForeignKey(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(p => p.ReturnRequest)
                    .WithMany(rr => rr.Payments)
                    .HasForeignKey(p => p.ReturnRequestId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 12. TransactionHistory
            // ==========================================
            modelBuilder.Entity<TransactionHistory>(builder =>
            {
                builder.HasKey(th => th.Id);
                builder.Property(th => th.Amount).HasPrecision(12, 2);
                builder.Property(th => th.TransactionCode).HasMaxLength(100);
                builder.Property(th => th.Description).HasMaxLength(255);

                builder.Property(th => th.ReferenceType)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                builder.Property(th => th.TransactionType)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                builder.HasOne(th => th.User)
                    .WithMany(u => u.TransactionHistories)
                    .HasForeignKey(th => th.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 13. Feedback (Quan hệ 1-1 với OrderDetail)
            // ==========================================
            modelBuilder.Entity<Feedback>(builder =>
            {
                builder.HasKey(f => f.Id);
                builder.Property(f => f.ImageUrl).HasMaxLength(255);

                builder.Property(f => f.Type)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(FeedbackType.BOOK);

                builder.HasIndex(f => f.OrderDetailId).IsUnique();

                builder.HasOne(f => f.Shop)
                    .WithMany(s => s.Feedbacks)
                    .HasForeignKey(f => f.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(f => f.OrderDetail)
                    .WithOne(od => od.Feedback)
                    .HasForeignKey<Feedback>(f => f.OrderDetailId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 14. Response (Quan hệ 1-1 với Feedback)
            // ==========================================
            modelBuilder.Entity<Response>(builder =>
            {
                builder.HasKey(r => r.Id);
                builder.Property(r => r.Content).IsRequired();
                builder.Property(r => r.ImageUrl).HasMaxLength(255);

                builder.HasIndex(r => r.FeedbackId).IsUnique();

                builder.HasOne(r => r.Feedback)
                    .WithOne(f => f.Response)
                    .HasForeignKey<Response>(r => r.FeedbackId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(r => r.Shop)
                    .WithMany(s => s.Responses)
                    .HasForeignKey(r => r.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 15. Chat
            // ==========================================
            modelBuilder.Entity<Chat>(builder =>
            {
                builder.HasKey(c => c.Id);

                builder.HasOne(c => c.User)
                    .WithMany(u => u.Chats)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(c => c.Shop)
                    .WithMany(s => s.Chats)
                    .HasForeignKey(c => c.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 16. Message
            // ==========================================
            modelBuilder.Entity<Message>(builder =>
            {
                builder.HasKey(m => m.Id);
                builder.Property(m => m.ImageUrl).HasMaxLength(255);
                builder.Property(m => m.IsRead).HasDefaultValue(false);

                builder.HasOne(m => m.Chat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 17. Notification
            // ==========================================
            modelBuilder.Entity<Notification>(builder =>
            {
                builder.HasKey(n => n.Id);
                builder.Property(n => n.ImageUrl).HasMaxLength(255);
                builder.Property(n => n.IsRead).HasDefaultValue(false);

                builder.Property(n => n.Type)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(NotificationType.SYSTEM);

                builder.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 18. UserSession
            // ==========================================
            modelBuilder.Entity<UserSession>(builder =>
            {
                builder.HasKey(us => us.Id);
                builder.Property(us => us.RefreshToken).IsRequired().HasMaxLength(255);
                builder.Property(us => us.IpAddress).HasMaxLength(45);
                builder.Property(us => us.DeviceInfo).HasMaxLength(255);
                builder.Property(us => us.IsRevoked).HasDefaultValue(false);

                builder.HasOne(us => us.User)
                    .WithMany(u => u.UserSessions)
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
