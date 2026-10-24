using Microsoft.EntityFrameworkCore;
using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Host> Hosts => Set<Host>();
    public DbSet<VisitPurpose> VisitPurposes => Set<VisitPurpose>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRoles(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureHosts(modelBuilder);
        ConfigurePurposes(modelBuilder);
        ConfigureVisitors(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        ConfigureProcessedMessages(modelBuilder);
    }

    private static void ConfigureRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            entity.HasIndex(x => x.Username)
                .IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureHosts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Host>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.Property(x => x.Department)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.FullName);
        });
    }

    private static void ConfigurePurposes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitPurpose>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });
    }

    private static void ConfigureVisitors(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.VisitorName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CompanyName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.MobileNumber)
                .IsRequired()
                .HasMaxLength(16);

            entity.Property(x => x.Email)
                .HasMaxLength(254);

            entity.Property(x => x.Remarks)
                .HasMaxLength(500);

            entity.Property(x => x.VisitDate)
                .HasColumnType("datetime2");

            entity.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            entity.Property(x => x.UpdatedAt)
                .HasColumnType("datetime2");

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.RowVersion)
                .IsRowVersion();

            entity.HasIndex(x => x.VisitDate);
            entity.HasIndex(x => x.VisitorName);
            entity.HasIndex(x => x.HostId);

            entity.HasOne(x => x.Host)
                .WithMany(x => x.Visitors)
                .HasForeignKey(x => x.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Purpose)
                .WithMany(x => x.Visitors)
                .HasForeignKey(x => x.PurposeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CorrelationId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Source)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.Timestamp)
                .HasColumnType("datetime2");

            entity.HasOne(x => x.Visitor)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.VisitorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PerformedByUser)
                .WithMany()
                .HasForeignKey(x => x.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProcessedMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.HasKey(x => x.MessageId);

            entity.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.ProcessedAt)
                .HasColumnType("datetime2");

            entity.HasOne<Visitor>()
                .WithMany()
                .HasForeignKey(x => x.VisitorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}