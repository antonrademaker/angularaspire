using Microsoft.EntityFrameworkCore;

namespace Shared.UserManagement;

/// <summary>
/// User service interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get a user by their ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user by their email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="user">User to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user</returns>
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing user
    /// </summary>
    /// <param name="user">User to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user</returns>
    Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user by ID
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Basic user service implementation
/// </summary>
public class UserService : IUserService
{
    private readonly UserDbContext _context;

    public UserService(UserDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User? user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
