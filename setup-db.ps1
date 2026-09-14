# ============================================================
# BOOKVERSE BE2 - Setup Database Script
# Chạy script này để tạo database và dữ liệu mẫu test Swagger
# ============================================================

Write-Host "`n[1/2] Applying EF Core migrations..." -ForegroundColor Cyan
dotnet ef database update --project BookManagement.Repository --startup-project BookManagement.Api

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[ERROR] Migration failed! Kiem tra lai SQL Server va connection string." -ForegroundColor Red
    Write-Host "Connection string: Server=localhost;Database=BookManagementDb;User Id=sa;Password=12345;TrustServerCertificate=True;" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n[OK] Migration applied successfully!" -ForegroundColor Green

Write-Host "`n[2/2] Inserting seed data..." -ForegroundColor Cyan

$connectionString = "Server=localhost;Database=BookManagementDb;User Id=sa;Password=12345;TrustServerCertificate=True;"

$sql = @"
-- ============================================================
-- SEED DATA FOR SWAGGER TESTING
-- ============================================================

-- Xoa du lieu cu neu ton tai (de re-run script nhieu lan)
DELETE FROM Messages;
DELETE FROM Chats;
DELETE FROM [Responses];
DELETE FROM Feedbacks;
DELETE FROM ReturnRequests;
DELETE FROM Payments;
DELETE FROM TransactionHistories;
DELETE FROM Deliveries;
DELETE FROM OrderDetails;
DELETE FROM Orders;
DELETE FROM Books;
DELETE FROM Shops;
DELETE FROM Categories;
DELETE FROM Users;

-- Reset identity sequences
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Categories', RESEED, 0);
DBCC CHECKIDENT ('Shops', RESEED, 0);
DBCC CHECKIDENT ('Books', RESEED, 0);
DBCC CHECKIDENT ('Orders', RESEED, 0);
DBCC CHECKIDENT ('OrderDetails', RESEED, 0);
DBCC CHECKIDENT ('Deliveries', RESEED, 0);
DBCC CHECKIDENT ('Payments', RESEED, 0);
DBCC CHECKIDENT ('ReturnRequests', RESEED, 0);
DBCC CHECKIDENT ('Feedbacks', RESEED, 0);
DBCC CHECKIDENT ('Responses', RESEED, 0);
DBCC CHECKIDENT ('Chats', RESEED, 0);
DBCC CHECKIDENT ('Messages', RESEED, 0);

-- ============================================================
-- USERS (UserId = 1 = SHOP, UserId = 2 = DELIVER, UserId = 3 = CUSTOMER)
-- ============================================================
INSERT INTO Users (Username, Email, PasswordHash, FullName, Phone, Role, [Status], Address, CreatedAt)
VALUES
  ('shopowner1', 'shop1@test.com', 'HASH_PLACEHOLDER', N'Nguyễn Văn Shop', '0901234567', 'SHOP', 'ACTIVE', N'123 Lê Lợi, Q1, TP.HCM', GETUTCDATE()),
  ('deliver1',   'deliver1@test.com', 'HASH_PLACEHOLDER', N'Trần Văn Shipper', '0912345678', 'DELIVER', 'ACTIVE', NULL, GETUTCDATE()),
  ('customer1',  'cust1@test.com', 'HASH_PLACEHOLDER', N'Lê Thị Khách', '0923456789', 'CUSTOMER', 'ACTIVE', N'456 Nguyễn Huệ, Q1, TP.HCM', GETUTCDATE());

-- ============================================================
-- CATEGORIES
-- ============================================================
INSERT INTO Categories (CategoryName, Description, [Status])
VALUES
  (N'Văn học', N'Sách văn học trong và ngoài nước', 1),
  (N'Kỹ năng sống', N'Sách phát triển bản thân', 1),
  (N'Lập trình', N'Sách về công nghệ thông tin', 1),
  (N'Kinh tế', N'Sách kinh tế, tài chính', 1);

-- ============================================================
-- SHOP (cho UserId = 1)
-- ============================================================
INSERT INTO Shops (UserId, ShopName, Condition, Rating, CreatedAt)
VALUES (1, N'Nhà Sách Tri Thức', 'OPEN', 5.0, GETUTCDATE());

-- ============================================================
-- BOOKS (ShopId = 1)
-- ============================================================
INSERT INTO Books (ShopId, CategoryId, Title, Isbn, Author, Publisher, Price, StockQuantity, [Description], ImageUrl, PublishedYear, [Status], Rating)
VALUES
  (1, 1, N'Nhà Giả Kim', '978-604-1-001', N'Paulo Coelho', N'NXB Hội Nhà Văn', 85000, 50, N'Tiểu thuyết nổi tiếng về hành trình tìm kiếm vận mệnh', 'https://example.com/book1.jpg', 1988, 'ACTIVE', 4.8),
  (1, 2, N'Đắc Nhân Tâm', '978-604-1-002', N'Dale Carnegie', N'NXB Tổng Hợp', 70000, 30, N'Nghệ thuật giao tiếp và ảnh hưởng con người', 'https://example.com/book2.jpg', 1936, 'ACTIVE', 4.9),
  (1, 3, N'Clean Code', '978-604-1-003', N'Robert C. Martin', N'NXB Lao Động', 120000, 20, N'Viết code sạch và dễ bảo trì', 'https://example.com/book3.jpg', 2008, 'ACTIVE', 4.7),
  (1, 4, N'Nghĩ Giàu Làm Giàu', '978-604-1-004', N'Napoleon Hill', N'NXB Trẻ', 95000, 0, N'Bí quyết tư duy làm giàu', 'https://example.com/book4.jpg', 1937, 'EMPTY', 4.6);

-- ============================================================
-- ORDER (UserId = 3 = Customer, mua sách BookId=1 và BookId=2)
-- ============================================================
INSERT INTO Orders (UserId, TotalAmount, OrderStatus, ShippingAddress, Weight, Note, CreatedAt, UpdatedAt)
VALUES (3, 155000, 'PAID', N'789 Nguyễn Trãi, Q5, TP.HCM', 0.5, N'Gói cẩn thận', GETUTCDATE(), NULL);

INSERT INTO OrderDetails (OrderId, BookId, Quantity, UnitPrice, ReturnStatus)
VALUES
  (1, 1, 1, 85000, 'NONE'),
  (1, 2, 1, 70000, 'NONE');

-- ============================================================
-- FEEDBACK (từ Customer cho sách BookId=1, ShopId=1)
-- ============================================================
INSERT INTO Feedbacks (ShopId, OrderDetailId, Rating, Content, [Type], ImageUrl, CreatedAt)
VALUES (1, 1, 5, N'Sách rất hay, giao hàng nhanh!', 'BOOK', NULL, GETUTCDATE());

-- ============================================================
-- CHAT + MESSAGES (giữa Customer và Shop)
-- ============================================================
INSERT INTO Chats (UserId, ShopId, UpdatedAt)
VALUES (3, 1, GETUTCDATE());

INSERT INTO Messages (ChatId, SenderId, Content, ImageUrl, IsRead, CreatedAt)
VALUES
  (1, 3, N'Cho hỏi sách Nhà Giả Kim còn hàng không ạ?', NULL, 0, DATEADD(MINUTE, -5, GETUTCDATE())),
  (1, 1, N'Dạ còn ạ! Shop còn 50 cuốn bạn nhé.', NULL, 1, DATEADD(MINUTE, -3, GETUTCDATE())),
  (1, 3, N'Cho mình đặt 1 cuốn ạ!', NULL, 0, GETUTCDATE());
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $command.CommandTimeout = 60
    $command.ExecuteNonQuery() | Out-Null

    $connection.Close()

    Write-Host "`n[OK] Seed data inserted successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor White
    Write-Host " DATABASE READY! Du lieu da san sang de test Swagger." -ForegroundColor Green
    Write-Host "============================================================" -ForegroundColor White
    Write-Host ""
    Write-Host "Du lieu da tao:" -ForegroundColor Cyan
    Write-Host "  Users:       UserId=1 (SHOP), UserId=2 (DELIVER), UserId=3 (CUSTOMER)" -ForegroundColor White
    Write-Host "  Categories:  4 danh muc sach" -ForegroundColor White
    Write-Host "  Shops:       ShopId=1 (Nha Sach Tri Thuc)" -ForegroundColor White
    Write-Host "  Books:       4 cuon sach (BookId=1,2,3 ACTIVE; BookId=4 EMPTY)" -ForegroundColor White
    Write-Host "  Orders:      OrderId=1 (PAID, 2 cuon sach, TotalAmount=155000)" -ForegroundColor White
    Write-Host "  Feedbacks:   FeedbackId=1 (5 sao, chua co phan hoi)" -ForegroundColor White
    Write-Host "  Chats:       ChatId=1 (3 tin nhan, 2 tin chua doc)" -ForegroundColor White
    Write-Host ""
    Write-Host "Buoc tiep theo:" -ForegroundColor Cyan
    Write-Host "  1. Chay API: dotnet run --project BookManagement.Api" -ForegroundColor Yellow
    Write-Host "  2. Mo Swagger: http://localhost:5000/swagger" -ForegroundColor Yellow
    Write-Host "  3. Tao JWT Token tai jwt.io voi:" -ForegroundColor Yellow
    Write-Host "     - nameidentifier: '1' (SHOP) hoac '2' (DELIVER)" -ForegroundColor White
    Write-Host "     - role: 'SHOP' hoac 'DELIVER'" -ForegroundColor White
    Write-Host "     - iss: 'BookManagementApi'" -ForegroundColor White
    Write-Host "     - aud: 'BookManagementClient'" -ForegroundColor White
    Write-Host "     - exp: 9999999999" -ForegroundColor White
    Write-Host "     - Secret: SuperSecretKeyForBookManagementProjectThatIsVeryLongAndSecure123!" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "`n[ERROR] Seed data failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Kiem tra lai SQL Server co dang chay khong!" -ForegroundColor Yellow
}
