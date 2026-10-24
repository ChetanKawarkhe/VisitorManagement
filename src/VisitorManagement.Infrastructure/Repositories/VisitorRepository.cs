using Microsoft.EntityFrameworkCore;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Domain.Entities;
using VisitorManagement.Infrastructure.Persistence;

namespace VisitorManagement.Infrastructure.Repositories;

public sealed class VisitorRepository : IVisitorRepository
{
    private readonly ApplicationDbContext _context;

    public VisitorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Visitor?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Visitors
            .AsNoTracking()
            .Include(x => x.Host)
            .Include(x => x.Purpose)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<Visitor> Items, int TotalCount)> SearchAsync(
        string? search,
        string sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Visitor> query = _context.Visitors
            .AsNoTracking()
            .Include(x => x.Host)
            .Include(x => x.Purpose);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string searchText = search.Trim();

            query = query.Where(x =>
                x.VisitorName.Contains(searchText) ||
                x.CompanyName.Contains(searchText) ||
                x.MobileNumber.Contains(searchText));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        query = sortBy.ToLowerInvariant() switch
        {
            "visitorname" => descending
                ? query.OrderByDescending(x => x.VisitorName)
                    .ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.VisitorName)
                    .ThenBy(x => x.Id),

            "visitdate" => descending
                ? query.OrderByDescending(x => x.VisitDate)
                    .ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.VisitDate)
                    .ThenBy(x => x.Id),

            _ => query.OrderByDescending(x => x.VisitDate)
                .ThenByDescending(x => x.Id)
        };

        int safePage = Math.Max(page, 1);
        int safePageSize = Math.Clamp(pageSize, 1, 100);

        int skip = (int)Math.Min(
            ((long)safePage - 1) * safePageSize,
            int.MaxValue);

        var items = await query
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(
        Visitor visitor,
        CancellationToken cancellationToken = default)
    {
        await _context.Visitors.AddAsync(
            visitor,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Visitor visitor,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        _context.Visitors.Attach(visitor);

        var entry = _context.Entry(visitor);

        entry.State = EntityState.Modified;

        entry.Property(x => x.RowVersion).OriginalValue =
            expectedRowVersion;

        await _context.SaveChangesAsync(cancellationToken);

        entry.State = EntityState.Detached;
    }

    public async Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Visitors
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == id,
                cancellationToken);
    }
}