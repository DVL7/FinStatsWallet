namespace FinStatsWallet.Domain.Entities;

public class Account
{
   public long Id { get; private set; }

   public long UserId { get; private set; }

   public string Name { get; private set; } = null!;

   public string AccountType { get; private set; }

   public string CurrencyCode { get; private set; } = "PLN";

   public decimal OpeningBalance { get; private set; }

   public bool IsActive { get; private set; }

   public DateTimeOffset CreatedAt { get; private set; }

   public DateTimeOffset UpdatedAt { get; private set; }

   // Nawigacja
   public User User { get; private set; } = null!;
   

   public Account(long userId, string name, string type, string currencyCode, decimal openingBalance)
   {
      UserId = userId;
      Name = name;
      AccountType = type;
      CurrencyCode = currencyCode;
      OpeningBalance = openingBalance;
      IsActive = true;
      CreatedAt = DateTimeOffset.UtcNow;
      UpdatedAt = DateTimeOffset.UtcNow;
   }

   public void Deactivate()
   {
      IsActive = false;
      UpdatedAt = DateTimeOffset.UtcNow;
   }
}