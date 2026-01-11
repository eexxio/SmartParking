using System.Threading.Tasks;

namespace SmartParking.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendPaymentConfirmationAsync(string email, decimal amount);

        Task SendReservationConfirmationAsync(string email, string spotNumber);
    }
}