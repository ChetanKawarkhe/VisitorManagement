using VisitorManagement.Application.DTOs.Auth;

namespace VisitorManagement.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}