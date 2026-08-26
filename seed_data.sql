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
    (NEWID(), 'admin', 'admin@bookmanagement.com', '$2a$11$EjTnc3zQKJPdkXfPYn0vN.nboiLN2KE8ZzSvCUblR2WVhWQamN.pu', N'Hệ Thống Quản Trị Viên', '0900000000', N'Hà Nội, Việt Nam', 'ADMIN', 'ACTIVE', SYSDATETIMEOFFSET(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'customer1')
BEGIN
    INSERT INTO Users (Id, Username, Email, PasswordHash, FullName, Phone, Address, Role, Status, CreatedAt, IsDeleted) VALUES
    (NEWID(), 'customer1', 'nguyenngocanh066206@gmail.com', '$2a$11$EjTnc3zQKJPdkXfPYn0vN.nboiLN2KE8ZzSvCUblR2WVhWQamN.pu', N'Nguyễn Văn An', '0987654321', N'123 Nguyễn Huệ, Q1, TP.HCM', 'CUSTOMER', 'ACTIVE', SYSDATETIMEOFFSET(), 0),
    (NEWID(), 'customer2', 'customer2@gmail.com', '$2a$11$EjTnc3zQKJPdkXfPYn0vN.nboiLN2KE8ZzSvCUblR2WVhWQamN.pu', N'Trần Thị Bình', '0912345678', N'456 Lê Lợi, Q1, TP.HCM', 'CUSTOMER', 'ACTIVE', SYSDATETIMEOFFSET(), 0),
    (NEWID(), 'shopowner1', 'shopowner1@gmail.com', '$2a$11$EjTnc3zQKJPdkXfPYn0vN.nboiLN2KE8ZzSvCUblR2WVhWQamN.pu', N'Lê Văn Cường', '0933445566', N'789 Điện Biên Phủ, Bình Thạnh, TP.HCM', 'SHOP', 'ACTIVE', SYSDATETIMEOFFSET(), 0),
    (NEWID(), 'shopowner2', 'shopowner2@gmail.com', '$2a$11$EjTnc3zQKJPdkXfPYn0vN.nboiLN2KE8ZzSvCUblR2WVhWQamN.pu', N'Phạm Hoàng Nam', '0977889900', N'101 Cầu Giấy, Hà Nội', 'SHOP', 'ACTIVE', SYSDATETIMEOFFSET(), 0);
END;
GO

-- 3. Thêm Cửa hàng (Shops)
DECLARE @SO1Id UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE Username = 'shopowner1');
DECLARE @SO2Id UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE Username = 'shopowner2');

IF @SO1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Shops WHERE UserId = @SO1Id)
BEGIN
    INSERT INTO Shops (Id, UserId, ShopName, Condition, Rating, CreatedAt, IsDeleted) VALUES
    (NEWID(), @SO1Id, N'Nhà Sách Tri Thức Việt', 'ACTIVE', 4.8, SYSDATETIMEOFFSET(), 0);
END;

IF @SO2Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Shops WHERE UserId = @SO2Id)
BEGIN
    INSERT INTO Shops (Id, UserId, ShopName, Condition, Rating, CreatedAt, IsDeleted) VALUES
    (NEWID(), @SO2Id, N'Nhà Sách Nhã Nam Demo', 'ACTIVE', 4.9, SYSDATETIMEOFFSET(), 0);
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

-- 5. Thêm Thông báo mẫu (Notifications)
IF NOT EXISTS (SELECT 1 FROM Notifications)
BEGIN
    DECLARE @Customer1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Username = 'customer1');
    DECLARE @Customer2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Username = 'customer2');
    DECLARE @AdminUserId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Username = 'admin');

    IF @Customer1Id IS NOT NULL
    BEGIN
        INSERT INTO Notifications (Id, UserId, Type, Content, ImageUrl, IsRead, CreatedAt, IsDeleted) VALUES
        (NEWID(), @Customer1Id, 'ORDER_UPDATE', N'Đơn hàng #ORD-1001 của bạn đã được xác nhận thành công!', NULL, 0, SYSDATETIMEOFFSET(), 0),
        (NEWID(), @Customer1Id, 'PROMOTION', N'Khuyến mãi 20% cho đơn hàng tiếp theo mừng khai trương!', NULL, 0, SYSDATETIMEOFFSET(), 0),
        (NEWID(), @Customer1Id, 'SYSTEM', N'Chào mừng bạn đến với sàn thương mại điện tử BookVerse!', NULL, 1, SYSDATETIMEOFFSET(), 0);
    END;

    IF @Customer2Id IS NOT NULL
    BEGIN
        INSERT INTO Notifications (Id, UserId, Type, Content, ImageUrl, IsRead, CreatedAt, IsDeleted) VALUES
        (NEWID(), @Customer2Id, 'PROMOTION', N'Sách mới: Lập Trình C# ASP.NET Core Toàn Tập đã có hàng!', NULL, 0, SYSDATETIMEOFFSET(), 0);
    END;

    IF @AdminUserId IS NOT NULL
    BEGIN
        INSERT INTO Notifications (Id, UserId, Type, Content, ImageUrl, IsRead, CreatedAt, IsDeleted) VALUES
        (NEWID(), @AdminUserId, 'SYSTEM', N'Hệ thống máy chủ BookVerse đã hoàn tất cập nhật.', NULL, 0, SYSDATETIMEOFFSET(), 0);
    END;
END;
GO

-- 6. Thêm Đơn hàng & Chi tiết Đơn hàng (Orders & OrderDetails)
IF NOT EXISTS (SELECT 1 FROM Orders)
BEGIN
    DECLARE @Cust1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Username = 'customer1');
    DECLARE @Cust2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Username = 'customer2');
    DECLARE @Book1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Books WHERE Title = N'Đắc Nhân Tâm (Bản Cao Cấp)');
    DECLARE @Book2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Books WHERE Title = N'Nhà Giả Kim');
    DECLARE @Book3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Books WHERE Title = N'Lập Trình C# ASP.NET Core Toàn Tập');

    IF @Cust1 IS NOT NULL AND @Book1 IS NOT NULL
    BEGIN
        DECLARE @Order1Id UNIQUEIDENTIFIER = NEWID();
        DECLARE @Order2Id UNIQUEIDENTIFIER = NEWID();
        DECLARE @Order3Id UNIQUEIDENTIFIER = NEWID();

        -- Đơn 1: Đã giao thành công (DELIVERED) để test Doanh thu & Đổi trả
        INSERT INTO Orders (Id, UserId, TotalAmount, OrderStatus, ShippingAddress, Weight, Note, CreatedAt, IsDeleted) VALUES
        (@Order1Id, @Cust1, 214000, 'DELIVERED', N'123 Nguyễn Huệ, Q1, TP.HCM', 0.8, N'Giao giờ hành chính', SYSDATETIMEOFFSET(), 0);

        DECLARE @OD1Id UNIQUEIDENTIFIER = NEWID();
        DECLARE @OD2Id UNIQUEIDENTIFIER = NEWID();

        INSERT INTO OrderDetails (Id, OrderId, BookId, Quantity, UnitPrice, ReturnStatus) VALUES
        (@OD1Id, @Order1Id, @Book1, 1, 125000, 'NONE'),
        (@OD2Id, @Order1Id, @Book2, 1, 89000, 'NONE');

        -- Đơn 2: Đang chờ giao hàng (PENDING) để test GHN / Vận đơn
        INSERT INTO Orders (Id, UserId, TotalAmount, OrderStatus, ShippingAddress, Weight, Note, CreatedAt, IsDeleted) VALUES
        (@Order2Id, @Cust1, 250000, 'APPROVED', N'123 Nguyễn Huệ, Q1, TP.HCM', 1.0, N'Đóng gói cẩn thận', SYSDATETIMEOFFSET(), 0);

        INSERT INTO OrderDetails (Id, OrderId, BookId, Quantity, UnitPrice, ReturnStatus) VALUES
        (NEWID(), @Order2Id, @Book3, 1, 250000, 'NONE');

        -- Đơn 3: PENDING để test duyệt đơn & VNPAY
        INSERT INTO Orders (Id, UserId, TotalAmount, OrderStatus, ShippingAddress, Weight, Note, CreatedAt, IsDeleted) VALUES
        (@Order3Id, @Cust2, 125000, 'PENDING', N'456 Lê Lợi, Q1, TP.HCM', 0.5, N'Gọi trước khi giao', SYSDATETIMEOFFSET(), 0);

        INSERT INTO OrderDetails (Id, OrderId, BookId, Quantity, UnitPrice, ReturnStatus) VALUES
        (NEWID(), @Order3Id, @Book1, 1, 125000, 'NONE');

        -- Đánh giá Feedbacks (Test Suite 3.4)
        DECLARE @Shop1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Shops WHERE ShopName = N'Nhà Sách Tri Thức Việt');
        IF @Shop1 IS NOT NULL
        BEGIN
            DECLARE @Feedback1Id UNIQUEIDENTIFIER = NEWID();
            INSERT INTO Feedbacks (Id, ShopId, OrderDetailId, Rating, Content, Type, ImageUrl, CreatedAt, IsDeleted) VALUES
            (@Feedback1Id, @Shop1, @OD1Id, 5, N'Sách đóng gói rất đẹp, giao nhanh tuyệt vời!', 'BOOK', NULL, SYSDATETIMEOFFSET(), 0);

            -- Yêu cầu đổi trả mẫu (Test Suite 3.5 & 6.2)
            INSERT INTO ReturnRequests (Id, OrderDetailId, ReasonType, DetailedReason, Status, RefundAmount, CreatedAt) VALUES
            (NEWID(), @OD2Id, 'DAMAGED', N'Sách bị lỗi từ nhà in', 'PENDING', 89000, SYSDATETIMEOFFSET());
        END;
    END;
END;
GO

-- 7. Thêm Cuộc trò chuyện & Tin nhắn Chat mẫu (Test Suite 2)
IF NOT EXISTS (SELECT 1 FROM Chats)
BEGIN
    DECLARE @CustUser UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Username = 'customer1');
    DECLARE @Shop1Entity UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Shops WHERE ShopName = N'Nhà Sách Tri Thức Việt');

    IF @CustUser IS NOT NULL AND @Shop1Entity IS NOT NULL
    BEGIN
        DECLARE @ChatId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO Chats (Id, UserId, ShopId, CreatedAt, UpdatedAt, IsDeleted) VALUES
        (@ChatId, @CustUser, @Shop1Entity, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0);

        INSERT INTO Messages (Id, ChatId, SenderId, Content, ImageUrl, IsRead, CreatedAt, IsDeleted) VALUES
        (NEWID(), @ChatId, @CustUser, N'Chào Shop, cuốn Đắc Nhân Tâm bản cao cấp còn sẵn hàng không ạ?', NULL, 1, DATEADD(MINUTE, -10, SYSDATETIMEOFFSET()), 0),
        (NEWID(), @ChatId, (SELECT TOP 1 UserId FROM Shops WHERE Id = @Shop1Entity), N'Dạ chào bạn, sách bên mình luôn có sẵn và được bọc màng co cẩn thận bạn nhé!', NULL, 1, DATEADD(MINUTE, -5, SYSDATETIMEOFFSET()), 0),
        (NEWID(), @ChatId, @CustUser, N'Shop ơi kiểm tra giúp mình đơn hàng vừa đặt nhé!', NULL, 0, SYSDATETIMEOFFSET(), 0);
    END;
END;
GO


