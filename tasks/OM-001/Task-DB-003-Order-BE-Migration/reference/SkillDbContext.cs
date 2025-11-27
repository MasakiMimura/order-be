using Microsoft.EntityFrameworkCore;
using Candidate_BE.Models;

namespace Candidate_BE.Data
{
public class SkillDbContext : DbContext
{
    public SkillDbContext(DbContextOptions<SkillDbContext> options) : base(options) { }

    public DbSet<Candidate> Candidates { get; set; }
    public DbSet<Cloud> Clouds { get; set; }
    public DbSet<Database> Databases { get; set; }
    public DbSet<FrameworkBackend> FrameworksBackend { get; set; }
    public DbSet<FrameworkFrontend> FrameworksFrontend { get; set; }
    public DbSet<OS> OS { get; set; }
    public DbSet<ProgrammingLanguage> ProgrammingLanguages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Candidate>().ToTable("candidates");
        modelBuilder.Entity<Cloud>().ToTable("cloud");
        modelBuilder.Entity<Database>().ToTable("databases");
        modelBuilder.Entity<FrameworkBackend>().ToTable("frameworks_backend");
        modelBuilder.Entity<FrameworkFrontend>().ToTable("frameworks_frontend");
        modelBuilder.Entity<OS>().ToTable("os");
        modelBuilder.Entity<ProgrammingLanguage>().ToTable("programming_languages");
    }
}
}