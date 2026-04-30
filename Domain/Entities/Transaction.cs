namespace FinStatsWallet.Domain.Entities;

public class Transaction
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public long AccountId { get; private set; }
    public long? CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Direction { get; private set; }
    public string Note { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Transaction(long id, long userId, long accountId, long? categoryId, decimal amount, string direction,
        string note, DateTimeOffset occurredAt, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        Direction = direction;
        Note = note;
        OccurredAt = occurredAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}

