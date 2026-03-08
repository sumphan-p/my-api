-- ============================================================
-- AuthAPI: Full Schema
-- Run this script on SQL Server (localhost) with admin account
-- ============================================================

-- 1. Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AuthAPI')
BEGIN
    CREATE DATABASE [AuthAPI];
    PRINT 'Database [AuthAPI] created.';
END
ELSE
    PRINT 'Database [AuthAPI] already exists. Skipping.';
GO

USE [AuthAPI];
GO

-- ============================================================
-- 2. Users Table
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id                   INT IDENTITY(1,1) PRIMARY KEY,
        Username             NVARCHAR(100)     NOT NULL UNIQUE,
        Email                NVARCHAR(255)     NULL,
        PasswordHash         NVARCHAR(500)     NOT NULL,
        Role                 NVARCHAR(50)      NOT NULL DEFAULT 'user',
        LoginAttempts        INT               NOT NULL DEFAULT 0,
        LockedUntil          DATETIME2         NULL,
        PasswordResetToken   NVARCHAR(500)     NULL,
        PasswordResetExpires DATETIME2         NULL,
        CreatedAt            DATETIME2         NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX IX_Users_Email ON Users (Email) WHERE Email IS NOT NULL;
    CREATE INDEX IX_Users_PasswordResetToken ON Users (PasswordResetToken) WHERE PasswordResetToken IS NOT NULL;
    CREATE INDEX IX_Users_LockedUntil ON Users (LockedUntil) WHERE LockedUntil IS NOT NULL;

    PRINT 'Table [Users] created.';
END
GO

-- ============================================================
-- 3. RefreshTokens Table
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE RefreshTokens (
        Id        INT IDENTITY(1,1) PRIMARY KEY,
        UserId    INT           NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
        Token     NVARCHAR(500) NOT NULL UNIQUE,
        ExpiresAt DATETIME2     NOT NULL,
        IsRevoked BIT           NOT NULL DEFAULT 0,
        CreatedAt DATETIME2     NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_RefreshTokens_Token_IsRevoked ON RefreshTokens (Token, IsRevoked);
    CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens (UserId);

    PRINT 'Table [RefreshTokens] created.';
END
GO

-- ============================================================
-- 4. Clients Table
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE Clients (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        ClientId     NVARCHAR(100) NOT NULL UNIQUE,
        ClientSecret NVARCHAR(500) NOT NULL,
        Name         NVARCHAR(200) NOT NULL,
        IsActive     BIT           NOT NULL DEFAULT 1,
        CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE()
    );

    PRINT 'Table [Clients] created.';
END
GO

-- ============================================================
-- 5. AuditLogs Table
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId    INT            NULL REFERENCES Users(Id),
        Action    NVARCHAR(50)   NOT NULL,
        IpAddress NVARCHAR(45)   NULL,
        UserAgent NVARCHAR(500)  NULL,
        Details   NVARCHAR(1000) NULL,
        CreatedAt DATETIME2      NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_AuditLogs_UserId    ON AuditLogs (UserId);
    CREATE INDEX IX_AuditLogs_Action    ON AuditLogs (Action);
    CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs (CreatedAt);

    PRINT 'Table [AuditLogs] created.';
END
GO

PRINT '=== AuthAPI schema setup complete ===';
GO
