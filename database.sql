
/* =========================================================
   B2B WHOLESALE MANAGEMENT SYSTEM
   Database: WholesaleManagementDB
   SQL Server
   ========================================================= */

-- 1. CREATE DATABASE
IF DB_ID('WholesaleManagementDB') IS NULL
BEGIN
    CREATE DATABASE WholesaleManagementDB;
END
GO

USE WholesaleManagementDB;
GO

/* =========================================================
   2. DROP EXISTING TABLES (FOR RE-RUNNING THE SCRIPT)
   ========================================================= */

DROP TABLE IF EXISTS OrderDetail;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Agent;
DROP TABLE IF EXISTS Item;
GO

/* =========================================================
   3. CREATE ITEM TABLE
   ========================================================= */

CREATE TABLE Item
(
    ItemID INT IDENTITY(1,1) PRIMARY KEY,
    ItemName NVARCHAR(150) NOT NULL,
    Size NVARCHAR(50) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,

    CONSTRAINT CK_Item_Price
        CHECK (Price > 0),

    CONSTRAINT CK_Item_Stock
        CHECK (StockQuantity >= 0)
);
GO

/* =========================================================
   4. CREATE AGENT TABLE
   ========================================================= */

CREATE TABLE Agent
(
    AgentID INT IDENTITY(1,1) PRIMARY KEY,
    AgentName NVARCHAR(150) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,

    CONSTRAINT UQ_Agent_Phone UNIQUE (Phone)
);
GO

/* =========================================================
   5. CREATE USERS TABLE
   ========================================================= */

CREATE TABLE Users
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Password NVARCHAR(255) NOT NULL,
    IsLocked BIT NOT NULL DEFAULT 0,
    Role NVARCHAR(20) NOT NULL,
    AgentID INT NULL,

    CONSTRAINT UQ_Users_UserName UNIQUE (UserName),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),

    CONSTRAINT CK_Users_Role
        CHECK (Role IN ('Admin', 'Agent')),

    CONSTRAINT FK_Users_Agent
        FOREIGN KEY (AgentID)
        REFERENCES Agent(AgentID)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT CK_Users_AgentRole
        CHECK (
            (Role = 'Admin' AND AgentID IS NULL)
            OR
            (Role = 'Agent' AND AgentID IS NOT NULL)
        )
);
GO

/* =========================================================
   6. CREATE ORDERS TABLE
   ========================================================= */

CREATE TABLE Orders
(
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    OrderDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    AgentID INT NOT NULL,
    OrderStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',

    CONSTRAINT FK_Orders_Agent
        FOREIGN KEY (AgentID)
        REFERENCES Agent(AgentID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION,

    CONSTRAINT CK_Orders_Status
        CHECK (
            OrderStatus IN ('Pending', 'Shipped', 'Cancelled')
        )
);
GO

/* =========================================================
   7. CREATE ORDER DETAIL TABLE
   ========================================================= */

CREATE TABLE OrderDetail
(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    ItemID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitAmount DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderDetail_Orders
        FOREIGN KEY (OrderID)
        REFERENCES Orders(OrderID)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT FK_OrderDetail_Item
        FOREIGN KEY (ItemID)
        REFERENCES Item(ItemID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION,

    CONSTRAINT CK_OrderDetail_Quantity
        CHECK (Quantity > 0),

    CONSTRAINT CK_OrderDetail_UnitAmount
        CHECK (UnitAmount > 0),

    CONSTRAINT UQ_OrderDetail_Order_Item
        UNIQUE (OrderID, ItemID)
);
GO

/* =========================================================
   8. INSERT ITEM DATA (20 RECORDS)
   ========================================================= */

INSERT INTO Item (ItemName, Size, Price, StockQuantity)
VALUES
('Premium White Rice', '25kg', 450000, 100),
('Jasmine Rice', '10kg', 220000, 150),
('Cooking Oil', '5L', 185000, 200),
('Fish Sauce', '750ml', 45000, 300),
('Soy Sauce', '1L', 38000, 250),
('Instant Noodles', 'Pack of 30', 95000, 500),
('White Sugar', '10kg', 260000, 120),
('Black Pepper', '1kg', 180000, 80),
('Ground Coffee', '500g', 125000, 100),
('Green Tea', 'Box of 100', 85000, 150),
('Bottled Water', '24 bottles', 60000, 400),
('Canned Tuna', 'Pack of 24', 320000, 90),
('Tomato Sauce', '1kg', 65000, 180),
('Mayonnaise', '1kg', 95000, 120),
('Laundry Detergent', '5kg', 175000, 100),
('Dishwashing Liquid', '3L', 85000, 150),
('Paper Towels', 'Pack of 12', 110000, 200),
('Hand Soap', '5L', 130000, 100),
('Milk Powder', '900g', 210000, 75),
('Chocolate Biscuits', 'Box of 20', 140000, 160);
GO

/* =========================================================
   9. INSERT AGENT DATA (20 RECORDS)
   ========================================================= */

INSERT INTO Agent (AgentName, Address, Phone)
VALUES
('Nguyen Van An', 'District 1, Ho Chi Minh City', '0901000001'),
('Tran Thi Binh', 'District 3, Ho Chi Minh City', '0901000002'),
('Le Minh Cuong', 'Thu Duc City, Ho Chi Minh City', '0901000003'),
('Pham Hoang Dung', 'Binh Thanh, Ho Chi Minh City', '0901000004'),
('Vo Thanh Dat', 'District 7, Ho Chi Minh City', '0901000005'),
('Hoang Thi Em', 'Can Tho City', '0901000006'),
('Bui Quoc Phong', 'Da Nang City', '0901000007'),
('Dang Minh Hieu', 'Hai Chau, Da Nang', '0901000008'),
('Do Thi Huong', 'Nha Trang, Khanh Hoa', '0901000009'),
('Ngo Van Khang', 'Da Lat, Lam Dong', '0901000010'),
('Ly Thanh Long', 'Bien Hoa, Dong Nai', '0901000011'),
('Mai Thi Lan', 'Vung Tau, Ba Ria', '0901000012'),
('Truong Minh Nam', 'District 10, Ho Chi Minh City', '0901000013'),
('Dinh Quoc Oanh', 'Binh Duong Province', '0901000014'),
('Cao Van Phuc', 'Long An Province', '0901000015'),
('Phan Thi Quyen', 'District 5, Ho Chi Minh City', '0901000016'),
('Vu Minh Son', 'Hue City', '0901000017'),
('Ta Hoang Tai', 'Quy Nhon, Binh Dinh', '0901000018'),
('Chau Thi Uyen', 'District 12, Ho Chi Minh City', '0901000019'),
('Nguyen Thanh Vinh', 'Tay Ninh Province', '0901000020');
GO

/* =========================================================
   10. INSERT USERS DATA (20 RECORDS)
   Password values are demo placeholders.
   Replace with application-generated password hashes
   before using this database in production.
   ========================================================= */

INSERT INTO Users
    (UserName, Email, Password, IsLocked, Role, AgentID)
VALUES
('admin', 'admin@wholesale.com', 'DEMO_HASH_ADMIN', 0, 'Admin', NULL),
('agent01', 'agent01@wholesale.com', 'DEMO_HASH_01', 0, 'Agent', 1),
('agent02', 'agent02@wholesale.com', 'DEMO_HASH_02', 0, 'Agent', 2),
('agent03', 'agent03@wholesale.com', 'DEMO_HASH_03', 0, 'Agent', 3),
('agent04', 'agent04@wholesale.com', 'DEMO_HASH_04', 0, 'Agent', 4),
('agent05', 'agent05@wholesale.com', 'DEMO_HASH_05', 0, 'Agent', 5),
('agent06', 'agent06@wholesale.com', 'DEMO_HASH_06', 0, 'Agent', 6),
('agent07', 'agent07@wholesale.com', 'DEMO_HASH_07', 0, 'Agent', 7),
('agent08', 'agent08@wholesale.com', 'DEMO_HASH_08', 0, 'Agent', 8),
('agent09', 'agent09@wholesale.com', 'DEMO_HASH_09', 0, 'Agent', 9),
('agent10', 'agent10@wholesale.com', 'DEMO_HASH_10', 0, 'Agent', 10),
('agent11', 'agent11@wholesale.com', 'DEMO_HASH_11', 0, 'Agent', 11),
('agent12', 'agent12@wholesale.com', 'DEMO_HASH_12', 0, 'Agent', 12),
('agent13', 'agent13@wholesale.com', 'DEMO_HASH_13', 0, 'Agent', 13),
('agent14', 'agent14@wholesale.com', 'DEMO_HASH_14', 0, 'Agent', 14),
('agent15', 'agent15@wholesale.com', 'DEMO_HASH_15', 0, 'Agent', 15),
('agent16', 'agent16@wholesale.com', 'DEMO_HASH_16', 0, 'Agent', 16),
('agent17', 'agent17@wholesale.com', 'DEMO_HASH_17', 0, 'Agent', 17),
('agent18', 'agent18@wholesale.com', 'DEMO_HASH_18', 0, 'Agent', 18),
('agent19', 'agent19@wholesale.com', 'DEMO_HASH_19', 0, 'Agent', 19);
GO

/* =========================================================
   11. INSERT ORDERS DATA (20 RECORDS)
   ========================================================= */

INSERT INTO Orders (OrderDate, AgentID, OrderStatus)
VALUES
('2026-10-01', 1, 'Shipped'),
('2026-10-02', 2, 'Shipped'),
('2026-10-03', 3, 'Pending'),
('2026-10-04', 4, 'Shipped'),
('2026-10-05', 5, 'Pending'),
('2026-10-06', 6, 'Shipped'),
('2026-10-07', 7, 'Pending'),
('2026-10-08', 8, 'Shipped'),
('2026-10-09', 9, 'Pending'),
('2026-10-10', 10, 'Shipped'),
('2026-10-11', 11, 'Pending'),
('2026-10-12', 12, 'Shipped'),
('2026-10-13', 13, 'Pending'),
('2026-10-14', 14, 'Shipped'),
('2026-10-15', 15, 'Pending'),
('2026-10-16', 16, 'Shipped'),
('2026-10-17', 17, 'Pending'),
('2026-10-18', 18, 'Shipped'),
('2026-10-19', 19, 'Pending'),
('2026-10-20', 20, 'Shipped');
GO

/* =========================================================
   12. INSERT ORDER DETAIL DATA (20 RECORDS)
   ========================================================= */

INSERT INTO OrderDetail (OrderID, ItemID, Quantity, UnitAmount)
VALUES
(1, 1, 10, 450000),
(2, 2, 15, 220000),
(3, 3, 20, 185000),
(4, 4, 30, 45000),
(5, 5, 25, 38000),
(6, 6, 50, 95000),
(7, 7, 12, 260000),
(8, 8, 10, 180000),
(9, 9, 15, 125000),
(10, 10, 20, 85000),
(11, 11, 40, 60000),
(12, 12, 10, 320000),
(13, 13, 25, 65000),
(14, 14, 15, 95000),
(15, 15, 10, 175000),
(16, 16, 20, 85000),
(17, 17, 30, 110000),
(18, 18, 15, 130000),
(19, 19, 8, 210000),
(20, 20, 12, 140000);
GO

/* =========================================================
   13. CREATE INDEXES FOR COMMON QUERIES
   ========================================================= */

CREATE INDEX IX_Orders_AgentID
ON Orders(AgentID);

CREATE INDEX IX_Orders_OrderDate
ON Orders(OrderDate);

CREATE INDEX IX_OrderDetail_ItemID
ON OrderDetail(ItemID);

CREATE INDEX IX_Users_AgentID
ON Users(AgentID);
GO

/* =========================================================
   14. VERIFY TABLE DATA
   ========================================================= */

SELECT 'Item' AS TableName, COUNT(*) AS TotalRecords FROM Item
UNION ALL
SELECT 'Agent', COUNT(*) FROM Agent
UNION ALL
SELECT 'Orders', COUNT(*) FROM Orders
UNION ALL
SELECT 'OrderDetail', COUNT(*) FROM OrderDetail
UNION ALL
SELECT 'Users', COUNT(*) FROM Users;
GO

/* =========================================================
   15. VIEW SAMPLE DATA
   ========================================================= */

SELECT * FROM Item;
SELECT * FROM Agent;
SELECT * FROM Users;
SELECT * FROM Orders;
SELECT * FROM OrderDetail;
GO

/* =========================================================
   16. TEST MASTER-DETAIL RELATIONSHIP
   ========================================================= */

SELECT
    o.OrderID,
    o.OrderDate,
    a.AgentName,
    i.ItemName,
    od.Quantity,
    od.UnitAmount,
    (od.Quantity * od.UnitAmount) AS Subtotal,
    o.OrderStatus
FROM Orders o
INNER JOIN Agent a ON o.AgentID = a.AgentID
INNER JOIN OrderDetail od ON o.OrderID = od.OrderID
INNER JOIN Item i ON od.ItemID = i.ItemID
ORDER BY o.OrderID;
GO

/* =========================================================
   END OF DATABASE SCRIPT
   ========================================================= */
