using MailingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace MailingService.Data;

public class MailingDbContext : DbContext
{
    public MailingDbContext(DbContextOptions<MailingDbContext> options) : base(options)
    {
    }

    public DbSet<EmailLog> EmailLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipient).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });
    }
}