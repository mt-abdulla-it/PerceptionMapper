-- Perception Mapper — SQL Server version (for SSMS / Azure SQL / LocalDB)
-- DO NOT run database\Schema.sql in SQL Server — that file is SQLite-only.
--
-- Note: The running app uses SQLite (perceptionmapper.db). Use this script only if
-- you need SQL Server tables for documentation, viva, or a separate SSMS database.

IF OBJECT_ID(N'dbo.PracticeSessions', N'U') IS NOT NULL DROP TABLE dbo.PracticeSessions;
IF OBJECT_ID(N'dbo.AnalysisRecords', N'U') IS NOT NULL DROP TABLE dbo.AnalysisRecords;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users (
    Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username      NVARCHAR(64)      NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(512)     NOT NULL,
    Salt          NVARCHAR(128)     NOT NULL,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE dbo.AnalysisRecords (
    Id                     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId                 INT               NOT NULL,
    InputText              NVARCHAR(MAX)     NOT NULL,
    DetectedLanguage       NVARCHAR(32)      NOT NULL,
    DominantTone           NVARCHAR(32)      NOT NULL,
    ToneScoresJson         NVARCHAR(MAX)     NOT NULL,
    FlaggedBiasJson        NVARCHAR(MAX)     NOT NULL,
    FeedbackMessage        NVARCHAR(MAX)     NOT NULL,
    RephrasingSuggestions  NVARCHAR(MAX)     NOT NULL,
    AnalyzedAt             DATETIME2         NOT NULL CONSTRAINT DF_AnalysisRecords_AnalyzedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_AnalysisRecords_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
);
GO

CREATE TABLE dbo.PracticeSessions (
    Id                  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId              INT               NOT NULL,
    Scenario            NVARCHAR(64)      NOT NULL,
    DominantTone        NVARCHAR(32)      NOT NULL,
    CommunicationScore  INT               NOT NULL,
    DetectedLanguage    NVARCHAR(32)      NOT NULL,
    DurationSeconds     FLOAT             NOT NULL,
    MomentCount         INT               NOT NULL,
    InputPreview        NVARCHAR(200)     NOT NULL,
    MomentsJson         NVARCHAR(MAX)     NOT NULL,
    CreatedAt           DATETIME2         NOT NULL CONSTRAINT DF_PracticeSessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_PracticeSessions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
);
GO

-- Demo user is created automatically when you run PerceptionMapper.Api (SQLite).
-- For SQL Server, register via POST /api/auth/register or insert a hashed user manually.
