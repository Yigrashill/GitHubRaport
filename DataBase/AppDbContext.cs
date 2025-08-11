using GitHubRaport.Models;
using Microsoft.EntityFrameworkCore;

namespace GitHubRaport.DataBase;

public class AppDbContext : DbContext
{
    public DbSet<CommitEntity> Commits => Set<CommitEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<CommitEntity>(e =>
        {
            e.ToTable("Commits");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Repository).HasMaxLength(200).IsRequired();
            e.Property(x => x.Sha).HasMaxLength(100).IsRequired();
            e.Property(x => x.Message).IsRequired();
            e.Property(x => x.Committer).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.UserName, x.Repository, x.Sha }).IsUnique();
        });
    }
}
