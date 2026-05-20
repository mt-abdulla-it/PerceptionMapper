-- =============================================================================
-- SQLITE ONLY — do NOT run this file in SQL Server Management Studio (SSMS).
-- SSMS will error: "Incorrect syntax near the keyword 'IF'".
--
-- For SQL Server, use: database\Schema-SqlServer.sql
--
-- You usually do NOT need to run any script: start PerceptionMapper.Api and
-- EF Core creates perceptionmapper.db automatically.
-- =============================================================================
-- Perception Mapper - SQLite schema (reference / DB Browser for SQLite)
-- File: perceptionmapper.db (next to API executable, e.g. bin\Debug\net8.0\)

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Salt TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS AnalysisRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    InputText TEXT NOT NULL,
    DetectedLanguage TEXT NOT NULL,
    DominantTone TEXT NOT NULL,
    ToneScoresJson TEXT NOT NULL,
    FlaggedBiasJson TEXT NOT NULL,
    FeedbackMessage TEXT NOT NULL,
    RephrasingSuggestions TEXT NOT NULL,
    AnalyzedAt TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS PracticeSessions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    Scenario TEXT NOT NULL,
    DominantTone TEXT NOT NULL,
    CommunicationScore INTEGER NOT NULL,
    DetectedLanguage TEXT NOT NULL,
    DurationSeconds REAL NOT NULL,
    MomentCount INTEGER NOT NULL,
    InputPreview TEXT NOT NULL,
    MomentsJson TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Default demo user (created on first API run): username=demo, password=Demo@123
