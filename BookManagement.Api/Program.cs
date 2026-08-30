using BookManagement.Service.BackgroundServices;
using BookManagement.Api.Controllers;
using BookManagement.Api.Extensions;
using BookManagement.Api.Filters;
using BookManagement.Api.Hubs;
using BookManagement.Api.Middlewares;
using BookManagement.Repository.Data;
using BookManagement.Service.Admin;
using BookManagement.Service.Auth;
using BookManagement.Service.Book;
using BookManagement.Service.Cart;
using BookManagement.Service.Category;
using BookManagement.Service.Chat;
using BookManagement.Service.Cloudinary;
using BookManagement.Service.Delivery;
using BookManagement.Service.Email;
using BookManagement.Service.Feedback;
using BookManagement.Service.Idempotency;
using BookManagement.Service.JwtService;
using BookManagement.Service.Notification;
using BookManagement.Service.Order;
using BookManagement.Service.Payment;
using BookManagement.Service.Shipping;
using BookManagement.Service.Shop;
using BookManagement.Service.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers with JsonOptions
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

// 2. Add Custom Extensions (JWT & Swagger)
builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddSwaggerServices();

// 3. Configuration Options Registrations
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("EmailOptions"));
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

// 4. Domain & Infrastructure Services DI Registrations
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<ShippingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddSingleton<IIdempotencyService, IdempotencyService>();

builder.Services.AddHttpClient<MomoService>();
builder.Services.AddHttpClient<GhnService>();

// 6. Real-Time SignalR Notifiers & Middlewares
builder.Services.AddScoped<IChatRealtimeNotifier, ChatRealtimeNotifier>();
builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
// builder.Services.AddHostedService<OrderExpirationBackgroundService>(); // Tắt chạy ngầm theo yêu cầu người dùng

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
            else
            {
                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.columns 
                        WHERE object_id = OBJECT_ID(N'[Shops]') 
                        AND name = 'ViolationCount'
                    )
                    BEGIN
                        ALTER TABLE [Shops] ADD [ViolationCount] INT NOT NULL DEFAULT 0;
                    END");

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.columns 
                        WHERE object_id = OBJECT_ID(N'[Shops]') 
                        AND name = 'LockedUntil'
                    )
                    BEGIN
                        ALTER TABLE [Shops] ADD [LockedUntil] DATETIMEOFFSET NULL;
                    END");

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.columns 
                        WHERE object_id = OBJECT_ID(N'[Payments]') 
                        AND name = 'TransactionCode'
                    )
                    BEGIN
                        ALTER TABLE [Payments] ADD [TransactionCode] NVARCHAR(100) NULL;
                    END");

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'BookImages')
                    BEGIN
                        CREATE TABLE [BookImages] (
                            [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                            [BookId] UNIQUEIDENTIFIER NOT NULL,
                            [ImageUrl] NVARCHAR(500) NOT NULL,
                            [PublicId] NVARCHAR(200) NULL,
                            [IsCover] BIT NOT NULL DEFAULT 0,
                            [DisplayOrder] INT NOT NULL DEFAULT 0,
                            [IsDeleted] BIT NOT NULL DEFAULT 0,
                            [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                            [UpdatedAt] DATETIMEOFFSET NULL,
                            CONSTRAINT [FK_BookImages_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE CASCADE
                        );
                        CREATE NONCLUSTERED INDEX [IX_BookImages_BookId] ON [BookImages]([BookId]);
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.columns 
                            WHERE object_id = OBJECT_ID(N'[BookImages]') 
                            AND name = 'IsDeleted'
                        )
                        BEGIN
                            ALTER TABLE [BookImages] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
                        END
                    END");

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes 
                        WHERE object_id = OBJECT_ID(N'[Payments]') 
                        AND name = 'IX_Payments_TransactionCode'
                    )
                    BEGIN
                        EXEC('CREATE UNIQUE NONCLUSTERED INDEX [IX_Payments_TransactionCode] ON [Payments]([TransactionCode]) WHERE [TransactionCode] IS NOT NULL;');
                    END");

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes 
                        WHERE object_id = OBJECT_ID(N'[TransactionHistories]') 
                        AND name = 'IX_TransactionHistories_TransactionCode'
                    )
                    BEGIN
                        EXEC('CREATE UNIQUE NONCLUSTERED INDEX [IX_TransactionHistories_TransactionCode] ON [TransactionHistories]([TransactionCode]) WHERE [TransactionCode] IS NOT NULL;');
                    END");

                // Auto-sync CreatedAt and UpdatedAt dynamically for ALL tables in SQL Server database
                dbContext.Database.ExecuteSqlRaw(@"
                    DECLARE @TableName NVARCHAR(255);
                    DECLARE @Sql NVARCHAR(MAX);

                    DECLARE table_cursor CURSOR FOR 
                    SELECT name FROM sys.tables WHERE type = 'U' AND name NOT LIKE '__EF%';

                    OPEN table_cursor;
                    FETCH NEXT FROM table_cursor INTO @TableName;

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        SET @Sql = 'IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''' + @TableName + ''') AND name = ''CreatedAt'') ' +
                                   'BEGIN ALTER TABLE [' + @TableName + '] ADD [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(); END';
                        EXEC sp_executesql @Sql;

                        SET @Sql = 'IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''' + @TableName + ''') AND name = ''UpdatedAt'') ' +
                                   'BEGIN ALTER TABLE [' + @TableName + '] ADD [UpdatedAt] DATETIMEOFFSET NULL; END';
                        EXEC sp_executesql @Sql;

                        FETCH NEXT FROM table_cursor INTO @TableName;
                    END;

                    CLOSE table_cursor;
                    DEALLOCATE table_cursor;");
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
app.MapHub<ChatHub>("/hubs/chat");

app.Run();