/* Run AFTER database.sql. Replaces the DEMO_HASH_* placeholders with real PBKDF2 hashes.
   admin   / Admin@123
   agentNN / Agent@123
   (Demo only: same salt reused so the script is repeatable. The apps generate a random salt for new users.) */
USE WholesaleManagementDB;
GO
UPDATE Users SET Password = 'PBKDF2$100000$AQIDBAUGBwgJCgsMDQ4PEA==$7gQDaNbD2TJ9Tv/U3z+oOOw+byXCRpvOoV5EbjMrc1w=' WHERE Role = 'Admin';
UPDATE Users SET Password = 'PBKDF2$100000$AQIDBAUGBwgJCgsMDQ4PEA==$Fy1a6ETD5udc/uGGVvXitRzul4EqL+M5yCdP3thmi4s=' WHERE Role = 'Agent';
GO
SELECT UserName, Role, LEFT(Password, 20) AS PasswordPrefix FROM Users;
