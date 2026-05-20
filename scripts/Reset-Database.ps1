# Deletes the local SQLite DB so EF can recreate all tables (Users, AnalysisRecords, PracticeSessions).
# Stop the API before running.

$paths = @(
    "$PSScriptRoot\..\src\PerceptionMapper.Api\bin\Debug\net8.0\perceptionmapper.db",
    "$PSScriptRoot\..\src\PerceptionMapper.Api\bin\Release\net8.0\perceptionmapper.db"
)

foreach ($p in $paths) {
    if (Test-Path $p) {
        Remove-Item $p -Force
        Write-Host "Deleted: $p"
    }
}

Write-Host "Done. Restart PerceptionMapper.Api and login with demo / Demo@123"
