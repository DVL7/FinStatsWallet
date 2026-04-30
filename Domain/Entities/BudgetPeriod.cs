namespace FinStatsWallet.Domain.Entities;

public class BudgetPeriod
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string Name { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public string CurrencyCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public BudgetPeriod(long id, long userId, string name, DateTime periodStart, DateTime periodEnd, string currencyCode, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        Name = name;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        CurrencyCode = currencyCode;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
