using VisitorManagement.Application.DTOs.Auth;
using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Application.Interfaces;

public interface ITokenGenerator
{
    LoginResponse GenerateToken(User user);
}