using AccessRequestHub.Domain;
using AccessRequestHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestHub.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessRequest>()
            .HasIndex(a => a.ClientRequestId)
            .IsUnique();

        modelBuilder.Entity<AccessRequest>()
            .HasOne(a => a.Requester)
            .WithMany()
            .HasForeignKey(a => a.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.SystemOwner)
            .WithMany()
            .HasForeignKey(a => a.SystemOwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Manager)
            .WithMany()
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        var aliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var carolId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var danaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var erinId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        modelBuilder.Entity<User>().HasData(
            new User { Id = aliceId, Email = "alice@example.local", Name = "Alice", Role = UserRole.Requester, ManagerId = bobId },
            new User { Id = bobId, Email = "bob@example.local", Name = "Bob", Role = UserRole.Manager },
            new User { Id = carolId, Email = "carol@example.local", Name = "Carol", Role = UserRole.SystemOwner },
            new User { Id = danaId, Email = "dana@example.local", Name = "Dana", Role = UserRole.SystemOwner },
            new User { Id = erinId, Email = "erin@example.local", Name = "Erin", Role = UserRole.Admin }
        );

        var crmId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
        var financeId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");

        modelBuilder.Entity<Application>().HasData(
            new Application { Id = crmId, Name = "CRM", SystemOwnerId = carolId },
            new Application { Id = financeId, Name = "Finance Portal", SystemOwnerId = danaId }
        );
    }
}