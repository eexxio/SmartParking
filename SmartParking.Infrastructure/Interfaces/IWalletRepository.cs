using SmartParking.Domain;

namespace SmartParking.Infrastructure.Interfaces;

public interface IWalletRepository
{
    UserWallet Create(UserWallet wallet);
    UserWallet? GetByUserId(Guid userId);
    void UpdateBalance(Guid walletId, decimal newBalance);
}
