using SmartParking.Domain.Entities;

namespace SmartParking.Application.Interfaces;

public interface IReservationService
{
    Reservation CreateReservation(Guid userId, Guid spotId, int cancellationTimeoutMinutes = 15);

    void ConfirmReservation(Guid reservationId);
    void CancelReservation(Guid reservationId);
    void CompleteReservation(Guid reservationId);

    IReadOnlyList<Reservation> GetUserReservations(Guid userId);

    int CheckAndApplyTimeoutPenalties();

    Reservation GetReservation(Guid reservationId);
}
