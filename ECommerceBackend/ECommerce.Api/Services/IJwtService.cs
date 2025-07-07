using ECommerce.Core.Entities;

namespace ECommerce.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    string? GetUserIdFromToken(string token);
}