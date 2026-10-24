using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Application.Interfaces;

public interface IPasswordVerifier
{
    bool VerifyPassword(User user, string password);
}