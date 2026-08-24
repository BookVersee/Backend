using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookStore.BE2.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<TransactionHistory> TransactionHistories => Set<TransactionHistory>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Enum conversions
        mb.Entity<User>().Property(e => e.Role).HasConversion<string>();
        mb.Entity<User>().Property(e => e.Status).HasConversion<string>();
        mb.Entity<Shop>().Property(e => e.Condition).HasConversion<string>();
        mb.Entity<Book>().Property(e => e.Status).HasConversion<string>();
        mb.Entity<Order>().Property(e => e.OrderStatus).HasConversion<string>();
        mb.Entity<OrderDetail>().Property(e => e.ReturnStatus).HasConversion<string>();
        mb.Entity<Delivery>().Property(e => e.Status).HasConversion<string>();
        mb.Entity<Payment>().Property(e => e.PaymentType).HasConversion<string>();
        mb.Entity<Payment>().Property(e => e.Method).HasConversion<string>();
        mb.Entity<Payment>().Property(e => e.Status).HasConversion<string>();
        mb.Entity<ReturnRequest>().Property(e => e.ReasonType).HasConversion<string>();
        mb.Entity<ReturnRequest>().Property(e => e.Status).HasConversion<string>();
        mb.Entity<TransactionHistory>().Property(e => e.ReferenceType).HasConversion<string>();
        mb.Entity<TransactionHistory>().Property(e => e.TransactionType).HasConversion<string>();
        mb.Entity<Feedback>().Property(e => e.Type).HasConversion<string>();

        // Explicit Primary Keys
        mb.Entity<User>().HasKey(e => e.UserId);
        mb.Entity<Category>().HasKey(e => e.CategoryId);
        mb.Entity<Shop>().HasKey(e => e.ShopId);
        mb.Entity<Book>().HasKey(e => e.BookId);
        mb.Entity<Order>().HasKey(e => e.OrderId);
        mb.Entity<OrderDetail>().HasKey(e => e.OrderDetailId);
        mb.Entity<Delivery>().HasKey(e => e.DeliveryId);
        mb.Entity<Payment>().HasKey(e => e.PaymentId);
        mb.Entity<ReturnRequest>().HasKey(e => e.ReturnRequestId);
        mb.Entity<TransactionHistory>().HasKey(e => e.TransactionId);
        mb.Entity<Feedback>().HasKey(e => e.FeedbackId);
        mb.Entity<Response>().HasKey(e => e.ResponseId);
        mb.Entity<Chat>().HasKey(e => e.ChatId);
        mb.Entity<Message>().HasKey(e => e.MessageId);

        // Decimal Precision
        mb.Entity<Book>().Property(b => b.Price).HasPrecision(10, 2);
        mb.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(12, 2);
        mb.Entity<Order>().Property(o => o.Weight).HasPrecision(8, 2);
        mb.Entity<OrderDetail>().Property(od => od.UnitPrice).HasPrecision(10, 2);
        mb.Entity<Delivery>().Property(d => d.ShipFee).HasPrecision(10, 2);
        mb.Entity<Payment>().Property(p => p.Amount).HasPrecision(12, 2);
        mb.Entity<ReturnRequest>().Property(r => r.RefundAmount).HasPrecision(12, 2);
        mb.Entity<TransactionHistory>().Property(t => t.Amount).HasPrecision(12, 2);

        // Unique Constraints
        mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<Shop>().HasIndex(s => s.UserId).IsUnique();
        mb.Entity<Book>().HasIndex(b => b.Isbn).IsUnique();
        mb.Entity<ReturnRequest>().HasIndex(r => r.OrderDetailId).IsUnique();
        mb.Entity<Feedback>().HasIndex(f => f.OrderDetailId).IsUnique();
        mb.Entity<Response>().HasIndex(r => r.FeedbackId).IsUnique();

        // 1-1 Relationships
        mb.Entity<Shop>()
            .HasOne(s => s.User)
            .WithOne(u => u.Shop)
            .HasForeignKey<Shop>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<ReturnRequest>()
            .HasOne(r => r.OrderDetail)
            .WithOne(od => od.ReturnRequest)
            .HasForeignKey<ReturnRequest>(r => r.OrderDetailId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Feedback>()
            .HasOne(f => f.OrderDetail)
            .WithOne(od => od.Feedback)
            .HasForeignKey<Feedback>(f => f.OrderDetailId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Response>()
            .HasOne(r => r.Feedback)
            .WithOne(f => f.Response)
            .HasForeignKey<Response>(r => r.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1-N Relationships
        mb.Entity<Book>()
            .HasOne(b => b.Shop)
            .WithMany(s => s.Books)
            .HasForeignKey(b => b.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<OrderDetail>()
            .HasOne(od => od.Book)
            .WithMany(b => b.OrderDetails)
            .HasForeignKey(od => od.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Delivery>()
            .HasOne(d => d.Order)
            .WithMany(o => o.Deliveries)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Payment>()
            .HasOne(p => p.ReturnRequest)
            .WithMany(r => r.Payments)
            .HasForeignKey(p => p.ReturnRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        // Chat relationships - no cascade to avoid cycle (User → Chat → Message → User)
        mb.Entity<Chat>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Chat>()
            .HasOne(c => c.Shop)
            .WithMany()
            .HasForeignKey(c => c.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Message>()
            .HasOne(m => m.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // TransactionHistory - no cascade
        mb.Entity<TransactionHistory>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Feedback - Shop FK restrict
        mb.Entity<Feedback>()
            .HasOne(f => f.Shop)
            .WithMany()
            .HasForeignKey(f => f.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        // Response - Shop FK restrict
        mb.Entity<Response>()
            .HasOne(r => r.Shop)
            .WithMany()
            .HasForeignKey(r => r.ShopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
