using BookManagement.Api.Extensions;
using BookManagement.Api.Middlewares;
using BookManagement.Repository.Data;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Repositories;
using BookManagement.Service.Admin;
using BookManagement.Service.Auth;
using BookManagement.Service.Book;
using BookManagement.Service.Cart;
using BookManagement.Service.Category;
using BookManagement.Service.Chat;
using BookManagement.Service.Delivery;
using BookManagement.Service.Feedback;
using BookManagement.Service.JwtService;
using BookManagement.Service.Notification;
using BookManagement.Service.Order;
using BookManagement.Service.Payment;
using BookManagement.Service.Response;
using BookManagement.Service.ReturnRequest;
using BookManagement.Service.Shop;
using BookManagement.Service.Transaction;
using BookManagement.Service.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers with JsonStringEnumConverter
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

// 2. Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

// 3. Add Custom Extensions (JWT & Swagger)
builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddSwaggerServices();

// 4. Register Repositories and Business Services

// User & Auth
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<IUserService, UserService>();

// Book & Category
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Shop
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<IShopService, ShopService>();

// Cart
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();

// Order
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Feedback & Response
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IResponseService, ResponseService>();

// Notification
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Delivery
builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();

// Payment
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Return Request
builder.Services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
builder.Services.AddScoped<IReturnRequestService, ReturnRequestService>();

// Chat
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IChatService, ChatService>();

// Transaction
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

// Admin
builder.Services.AddScoped<IAdminService, AdminService>();

// 5. Register Middlewares
builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

// 5. CORS Policy Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                var host = new Uri(origin).Host;
                return host == "localhost" || host.EndsWith(".vercel.app");
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Auto-create database & tables if not exists on SQL Server
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connStr))
        {
            var masterConnBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr)
            {
                InitialCatalog = "master"
            };

            using (var masterConn = new Microsoft.Data.SqlClient.SqlConnection(masterConnBuilder.ConnectionString))
            {
                masterConn.Open();
                using (var cmd = masterConn.CreateCommand())
                {
                    cmd.CommandText = @"
                        IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BookManagementDb')
                        BEGIN
                            CREATE DATABASE [BookManagementDb];
                        END";
                    cmd.ExecuteNonQuery();
                }
            }

            var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();
            if (!dbCreator.HasTables())
            {
                dbCreator.CreateTables();
            }

            // Ensure any user with username starting with 'admin' has ADMIN role in DB
            dbContext.Database.ExecuteSqlRaw("UPDATE Users SET Role = 'ADMIN' WHERE LOWER(Username) LIKE 'admin%'");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB Init] {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
app.UseSwaggerAPI();
app.UseCors("AllowFrontend");

// Middlewares pipeline ordering is critical!
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
