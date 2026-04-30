namespace FinStatsWallet.Domain.Entities;

public class User
{
    public long Id { get; private set; }
    public string Login { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public User(long id, string login, string email, string passwordHash, string fullName, DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Login = login;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}

