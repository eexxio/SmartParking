namespace SmartParking.Business;

public interface IWalletService
{
    decimal GetBalance(Guid userId);
    void Deposit(Guid userId, decimal amount);
    void Withdraw(Guid userId, decimal amount);
    bool CanAfford(Guid userId, decimal amount);
}
