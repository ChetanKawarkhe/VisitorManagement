using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}