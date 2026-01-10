using System.Threading.Tasks;

namespace SmartParking.Business.Services
{
    public interface INotificationService
    {
        Task SendPaymentReceiptAsync(string email, decimal amount);
    }
}