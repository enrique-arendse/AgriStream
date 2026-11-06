CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARBINARY(32) NOT NULL,
    PasswordSalt VARBINARY(16) NOT NULL,
    SystemRole NVARCHAR(30) NOT NULL CHECK (SystemRole IN ('Farmer','Agronomist','Admin')),
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Users_Created DEFAULT SYSUTCDATETIME()
);
CREATE TABLE dbo.DeviceRegistry (
    DeviceId NVARCHAR(100) PRIMARY KEY,
    OperationalStatus NVARCHAR(50) NOT NULL,
    LastCommandUtc DATETIME2 NULL,
    LastCommandId NVARCHAR(64) NULL
);
-- Create the first user with the companion PasswordHashGenerator tool or a one-time
-- C# PBKDF2 script. Never store plaintext passwords. See README for seed instructions.
