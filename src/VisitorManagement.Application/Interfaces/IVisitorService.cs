using VisitorManagement.Application.DTOs.Visitors;

namespace VisitorManagement.Application.Interfaces;

public interface IVisitorService
{
    Task<VisitorResponse> CreateAsync(
        CreateVisitorRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<VisitorResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VisitorResponse>> SearchAsync(
        VisitorSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<VisitorResponse> UpdateAsync(
        int id,
        UpdateVisitorRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);
}