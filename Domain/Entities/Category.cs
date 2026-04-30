namespace FinStatsWallet.Domain.Entities;

public class Category
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string Name { get; private set; }
    public string CategoryType { get; private set; }
    public long? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Category(long id, long userId, string name, string categoryType, long? parentCategoryId, bool isActive, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        Name = name;
        CategoryType = categoryType;
        ParentCategoryId = parentCategoryId;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}

