using Microsoft.AspNetCore.Identity;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Infrastructure.Authentication;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordVerifier(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public bool VerifyPassword(User user, string password)
    {
        if (user is null ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        try
        {
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}