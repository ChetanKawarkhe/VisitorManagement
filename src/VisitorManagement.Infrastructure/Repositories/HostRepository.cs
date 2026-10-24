using Microsoft.EntityFrameworkCore;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Domain.Entities;
using VisitorManagement.Infrastructure.Persistence;

namespace VisitorManagement.Infrastructure.Repositories;

public sealed class HostRepository : IHostRepository
{
    private readonly ApplicationDbContext _context;

    public HostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Host>> SearchActiveAsync(
        string? search,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Host> query = _context.Hosts
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string searchText = search.Trim();

            query = query.Where(x =>
                x.FullName.Contains(searchText) ||
                x.Department.Contains(searchText));
        }

        return await query
            .OrderBy(x => x.FullName)
            .Take(Math.Clamp(limit, 1, 10))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAndActiveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Hosts
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == id && x.IsActive,
                cancellationToken);
    }
}