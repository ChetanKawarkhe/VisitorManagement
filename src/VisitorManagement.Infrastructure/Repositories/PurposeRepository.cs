using Microsoft.EntityFrameworkCore;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Domain.Entities;
using VisitorManagement.Infrastructure.Persistence;

namespace VisitorManagement.Infrastructure.Repositories;

public sealed class PurposeRepository : IPurposeRepository
{
    private readonly ApplicationDbContext _context;

    public PurposeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<VisitPurpose>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.VisitPurposes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAndActiveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.VisitPurposes
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == id && x.IsActive,
                cancellationToken);
    }
}