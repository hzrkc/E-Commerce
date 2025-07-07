using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}