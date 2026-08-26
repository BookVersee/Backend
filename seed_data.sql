USE [BookManagementDb];
GO

-- 1. Thêm Thể loại Sách (Categories)
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = N'Văn Học')
BEGIN
    INSERT INTO Categories (Id, CategoryName, Status, IsDeleted) VALUES 
    (NEWID(), N'Văn Học', 1, 0),
    (NEWID(), N'Kinh Tế & Quản Trị', 1, 0),
    (NEWID(), N'Công Nghệ Thông Tin', 1, 0),
    (NEWID(), N'Tâm Lý & Kỹ Năng Sống', 1, 0),
    (NEWID(), N'Ngoại Ngữ & Du Học', 1, 0);
END;
GO

-- 2. Thêm Người dùng mẫu (Users) - Mật khẩu chung: Password123!
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Id, Username, Email, PasswordHash, FullName, Phone, Address, Role, Status, CreatedAt, IsDeleted) VALUES
    (NEWID(), 'admin', 'admin@bookmanagement.com', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', N'Hệ Thống Quản Trị Viên', '0900000000', N'Hà Nội, Việt Nam', 'ADMIN', 'ACTIVE', SYSDATETIMEOFFSET(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'customer1')
BEGIN
    INSERT INTO Users (Id, Username, Email, PasswordHash, FullName, Phone, Address, Role, Status, CreatedAt, IsDeleted) VALUES
    (NEWID(), 'customer1', 'nguyenngocanh066206@gmail.com', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', N'Nguyễn Văn An', '0987654321', N'123 Nguyễn Huệ, Q1, TP.HCM', 'CUSTOMER', 'ACTIVE', SYSDATETIMEOFFSET(), 0),
    (NEWID(), 'customer2', 'customer2@gmail.com', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', N'Trần Thị Bình', '0912345678', N'456 Lê Lợi, Q1, TP.HCM', 'CUSTOMER', 'ACTIVE', SYSDATETIMEOFFSET(), 0),
    (NEWID(), 'shopowner1', 'shopowner1@gmail.com', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', N'Lê Văn Cường', '0933445566', N'789 Điện Biên Phủ, Bình Thạnh, TP.HCM', 'SHOP', 'ACTIVE', SYSDATETIMEOFFSET(), 0),
    (NEWID(), 'shopowner2', 'shopowner2@gmail.com', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', N'Phạm Hoàng Nam', '0977889900', N'101 Cầu Giấy, Hà Nội', 'SHOP', 'ACTIVE', SYSDATETIMEOFFSET(), 0);
END;
GO

-- 3. Thêm Cửa hàng (Shops)
IF NOT EXISTS (SELECT 1 FROM Shops WHERE ShopName = N'Nhà Sách Tri Thức Việt')
BEGIN
    DECLARE @SO1Id UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE Username = 'shopowner1');
    DECLARE @SO2Id UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE Username = 'shopowner2');

    IF @SO1Id IS NOT NULL
    BEGIN
        INSERT INTO Shops (Id, UserId, ShopName, Condition, Rating, CreatedAt, IsDeleted) VALUES
        (NEWID(), @SO1Id, N'Nhà Sách Tri Thức Việt', 'ACTIVE', 4.8, SYSDATETIMEOFFSET(), 0);
    END;

    IF @SO2Id IS NOT NULL
    BEGIN
        INSERT INTO Shops (Id, UserId, ShopName, Condition, Rating, CreatedAt, IsDeleted) VALUES
        (NEWID(), @SO2Id, N'Nhà Sách Nhã Nam Demo', 'ACTIVE', 4.9, SYSDATETIMEOFFSET(), 0);
    END;
END;
GO

-- 4. Thêm Sản phẩm Sách (Books)
IF NOT EXISTS (SELECT 1 FROM Books WHERE Title = N'Đắc Nhân Tâm (Bản Cao Cấp)')
BEGIN
    DECLARE @S1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Shops WHERE ShopName = N'Nhà Sách Tri Thức Việt');
    DECLARE @S2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Shops WHERE ShopName = N'Nhà Sách Nhã Nam Demo');
    DECLARE @CatId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Categories WHERE CategoryName = N'Văn Học');

    IF @S1Id IS NOT NULL AND @CatId IS NOT NULL
    BEGIN
        INSERT INTO Books (Id, ShopId, CategoryId, Title, Author, Price, StockQuantity, Isbn, Status, ImageUrl, Rating, CreatedAt, IsDeleted) VALUES
        (NEWID(), @S1Id, @CatId, N'Đắc Nhân Tâm (Bản Cao Cấp)', 'Dale Carnegie', 125000, 100, '9786045638210', 'ACTIVE', 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c', 5.0, SYSDATETIMEOFFSET(), 0),
        (NEWID(), @S1Id, @CatId, N'Nhà Giả Kim', 'Paulo Coelho', 89000, 50, '9786045638211', 'ACTIVE', 'https://images.unsplash.com/photo-1512820790803-83ca734da794', 4.8, SYSDATETIMEOFFSET(), 0);
    END;

    IF @S2Id IS NOT NULL AND @CatId IS NOT NULL
    BEGIN
        INSERT INTO Books (Id, ShopId, CategoryId, Title, Author, Price, StockQuantity, Isbn, Status, ImageUrl, Rating, CreatedAt, IsDeleted) VALUES
        (NEWID(), @S2Id, @CatId, N'Lập Trình C# ASP.NET Core Toàn Tập', 'Microsoft Press', 250000, 80, '9786045638212', 'ACTIVE', 'https://images.unsplash.com/photo-1532012197267-da84d127e765', 5.0, SYSDATETIMEOFFSET(), 0);
    END;
END;
GO
