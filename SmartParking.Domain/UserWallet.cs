namespace SmartParking.Domain;

public class UserWallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public UserWallet(Guid userId, decimal initialBalance)
    {
        ValidateBalance(initialBalance);

        Id = Guid.NewGuid();
        UserId = userId;
        Balance = initialBalance;
        UpdatedAt = DateTime.UtcNow;
    }

    public UserWallet(Guid id, Guid userId, decimal balance, DateTime updatedAt)
    {
        ValidateBalance(balance);

        Id = id;
        UserId = userId;
        Balance = balance;
        UpdatedAt = updatedAt;
    }

    public bool CanWithdraw(decimal amount)
    {
        return amount > 0 && Balance >= amount;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Deposit amount must be greater than zero", nameof(amount));
        }

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Withdrawal amount must be greater than zero", nameof(amount));
        }

        if (!CanWithdraw(amount))
        {
            throw new InvalidOperationException($"Insufficient balance. Current balance: {Balance}, attempted withdrawal: {amount}");
        }

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateBalance(decimal balance)
    {
        if (balance < 0)
        {
            throw new ArgumentException("Balance cannot be negative", nameof(balance));
        }
    }
}
