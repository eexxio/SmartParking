using SmartParking.Domain.Entities;

namespace SmartParking.Business.Interfaces;

public interface IPenaltyService
{
    Penalty ApplyPenalty(Guid reservationId, decimal amount, string reason);
    IReadOnlyList<Penalty> GetUserPenalties(Guid userId);
    IReadOnlyList<Penalty> GetReservationPenalties(Guid reservationId);
}
