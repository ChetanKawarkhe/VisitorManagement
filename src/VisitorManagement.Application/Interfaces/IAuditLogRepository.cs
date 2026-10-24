using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetByVisitorIdAsync(
        int visitorId,
        CancellationToken cancellationToken = default);
}