using Microsoft.EntityFrameworkCore;
using PerceptionMapper.Domain.Entities;

namespace PerceptionMapper.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AnalysisRecord> AnalysisRecords => Set<AnalysisRecord>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(64);
        });

        modelBuilder.Entity<AnalysisRecord>(e =>
        {
            e.HasOne(a => a.User).WithMany(u => u.AnalysisRecords).HasForeignKey(a => a.UserId);
            e.Property(a => a.InputText).HasMaxLength(8000);
        });

        modelBuilder.Entity<PracticeSession>(e =>
        {
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
            e.Property(s => s.Scenario).HasMaxLength(64);
            e.Property(s => s.InputPreview).HasMaxLength(200);
        });
    }
}
