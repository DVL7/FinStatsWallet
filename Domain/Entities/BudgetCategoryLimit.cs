namespace FinStatsWallet.Domain.Entities;

public class BudgetCategoryLimit
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public long BudgetPeriodId { get; private set; }
    public long CategoryId { get; private set; }
    public decimal AmountLimit { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    // nawigacja
    public User User { get; private set; } = null!;
    public BudgetPeriod BudgetPeriod { get; private set; } = null!;
    public Category Category { get; private set; } = null!;

    public BudgetCategoryLimit(long id, long userId, long budgetPeriodId, long categoryId, decimal amountLimit, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        BudgetPeriodId = budgetPeriodId;
        CategoryId = categoryId;
        AmountLimit = amountLimit;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}