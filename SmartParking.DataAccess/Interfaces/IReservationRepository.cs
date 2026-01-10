using SmartParking.Domain.Entities;

namespace SmartParking.DataAccess.Interfaces;

public interface IReservationRepository
{
    void ValidateSpotForUser(Guid spotId, bool isEVUser);

    Reservation Create(Guid userId, Guid spotId, int cancellationTimeoutMinutes = 15);

    Reservation? GetById(Guid reservationId);
    IReadOnlyList<Reservation> GetByUserId(Guid userId);

    void Confirm(Guid reservationId);
    bool Cancel(Guid reservationId);   
    void Complete(Guid reservationId);

    IReadOnlyList<Reservation> GetExpiredPendingReservations();
}
