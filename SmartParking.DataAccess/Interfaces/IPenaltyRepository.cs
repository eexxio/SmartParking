using SmartParking.Domain.Entities;

namespace SmartParking.DataAccess.Interfaces;

public interface IPenaltyRepository
{
    Penalty Create(Penalty penalty);
    IReadOnlyList<Penalty> GetByReservationId(Guid reservationId);
    IReadOnlyList<Penalty> GetByUserId(Guid userId);
}
