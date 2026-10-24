using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VisitorManagement.Domain.Entities;
using VisitorManagement.Domain.Enums;

namespace VisitorManagement.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedHostsAsync(cancellationToken);
        await SeedPurposesAsync(cancellationToken);
        await SeedVisitorsAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var names = new[] { "Admin", "User" };

        foreach (var name in names)
        {
            if (!await _context.Roles.AnyAsync(x => x.Name == name, ct))
            {
                _context.Roles.Add(new Role { Name = name });
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        var passwordHasher = new PasswordHasher<User>();

        await CreateUserAsync(
            "admin",
            "Admin",
            "SeedUsers:AdminPassword",
            passwordHasher,
            ct);

        await CreateUserAsync(
            "user",
            "User",
            "SeedUsers:UserPassword",
            passwordHasher,
            ct);
    }

    private async Task CreateUserAsync(
        string username,
        string roleName,
        string passwordKey,
        PasswordHasher<User> hasher,
        CancellationToken ct)
    {
        if (await _context.Users.AnyAsync(x => x.Username == username, ct))
            return;

        var password = _configuration[passwordKey];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Required demo password configuration '{passwordKey}' is missing.");
        }

        var role = await _context.Roles
            .SingleAsync(x => x.Name == roleName, ct);

        var user = new User
        {
            Username = username,
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = hasher.HashPassword(user, password);

        _context.Users.Add(user);

        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedHostsAsync(CancellationToken ct)
    {
        var hosts = new[]
        {
            ("Amit Patil", "amit.patil@example.com", "IT"),
            ("Priya Sharma", "priya.sharma@example.com", "HR"),
            ("Rahul Deshmukh", "rahul.deshmukh@example.com", "Finance"),
            ("Sneha Kulkarni", "sneha.kulkarni@example.com", "Administration"),
            ("Rohit Joshi", "rohit.joshi@example.com", "Operations"),
            ("Pooja Mehta", "pooja.mehta@example.com", "HR"),
            ("Vikas Shah", "vikas.shah@example.com", "IT"),
            ("Neha Gupta", "neha.gupta@example.com", "Finance"),
            ("Sagar More", "sagar.more@example.com", "Security"),
            ("Anjali Rao", "anjali.rao@example.com", "Management")
        };

        foreach (var host in hosts)
        {
            if (!await _context.Hosts.AnyAsync(
                x => x.Email == host.Item2, ct))
            {
                _context.Hosts.Add(new Host
                {
                    FullName = host.Item1,
                    Email = host.Item2,
                    Department = host.Item3,
                    IsActive = true
                });
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedPurposesAsync(CancellationToken ct)
    {
        var purposes = new[]
        {
            "Meeting",
            "Interview",
            "Delivery",
            "Maintenance",
            "Other"
        };

        foreach (var purpose in purposes)
        {
            if (!await _context.VisitPurposes.AnyAsync(
                x => x.Name == purpose, ct))
            {
                _context.VisitPurposes.Add(new VisitPurpose
                {
                    Name = purpose,
                    IsActive = true
                });
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedVisitorsAsync(CancellationToken ct)
    {
        if (await _context.Visitors.AnyAsync(ct))
            return;

        var admin = await _context.Users
            .SingleAsync(x => x.Username == "admin", ct);

        var hosts = await _context.Hosts
            .OrderBy(x => x.Id)
            .Take(3)
            .ToListAsync(ct);

        var meeting = await _context.VisitPurposes
            .SingleAsync(x => x.Name == "Meeting", ct);

        var interview = await _context.VisitPurposes
            .SingleAsync(x => x.Name == "Interview", ct);

        var delivery = await _context.VisitPurposes
            .SingleAsync(x => x.Name == "Delivery", ct);

        var today = DateTime.Today;

        var visitors = new[]
        {
            new Visitor
            {
                VisitorName = "Demo Visitor One",
                CompanyName = "ABC Technologies",
                MobileNumber = "9876543210",
                Email = "visitor1@example.com",
                VisitDate = today.AddDays(1).AddHours(10),
                HostId = hosts[0].Id,
                PurposeId = meeting.Id,
                Status = VisitorStatus.Scheduled,
                CreatedByUserId = admin.Id
            },
            new Visitor
            {
                VisitorName = "Demo Visitor Two",
                CompanyName = "XYZ Solutions",
                MobileNumber = "9876543211",
                Email = "visitor2@example.com",
                VisitDate = today.AddDays(2).AddHours(11),
                HostId = hosts[1].Id,
                PurposeId = interview.Id,
                Status = VisitorStatus.Scheduled,
                CreatedByUserId = admin.Id
            },
            new Visitor
            {
                VisitorName = "Demo Visitor Three",
                CompanyName = "Sample Logistics",
                MobileNumber = "9876543212",
                Email = "visitor3@example.com",
                VisitDate = today.AddDays(3).AddHours(14),
                HostId = hosts[2].Id,
                PurposeId = delivery.Id,
                Status = VisitorStatus.Scheduled,
                CreatedByUserId = admin.Id
            }
        };

        _context.Visitors.AddRange(visitors);

        await _context.SaveChangesAsync(ct);
    }
}