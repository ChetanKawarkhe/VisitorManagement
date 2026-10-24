using VisitorManagement.Application.DTOs.Auth;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordVerifier passwordVerifier,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordVerifier = passwordVerifier;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        string username = request.Username.Trim();

        var user = await _userRepository.GetByUsernameAsync(
            username,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        bool passwordValid = _passwordVerifier.VerifyPassword(
            user,
            request.Password);

        if (!passwordValid)
        {
            return null;
        }

        return _tokenGenerator.GenerateToken(user);
    }
}