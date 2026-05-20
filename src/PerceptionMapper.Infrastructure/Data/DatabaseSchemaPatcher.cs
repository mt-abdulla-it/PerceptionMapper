using Microsoft.EntityFrameworkCore;

namespace PerceptionMapper.Infrastructure.Data;

/// <summary>
/// EF EnsureCreated does not add new tables to an existing SQLite file.
/// This patches older perceptionmapper.db files after PracticeSessions was introduced.
/// </summary>
public static class DatabaseSchemaPatcher
{
    public static async Task ApplyAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await TableExistsAsync(db, "PracticeSessions"))
            return;

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE PracticeSessions (
                Id INTEGER NOT NULL CONSTRAINT PK_PracticeSessions PRIMARY KEY AUTOINCREMENT,
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
                CONSTRAINT FK_PracticeSessions_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id)
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_PracticeSessions_UserId ON PracticeSessions (UserId);");
    }

    private static async Task<bool> TableExistsAsync(AppDbContext db, string tableName)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";
        var param = command.CreateParameter();
        param.ParameterName = "$name";
        param.Value = tableName;
        command.Parameters.Add(param);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }
}
