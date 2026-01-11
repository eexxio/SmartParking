using SmartParking.Domain.Entities;

namespace SmartParking.Infrastructure.Interfaces;

public interface IPenaltyRepository
{
    Penalty Create(Penalty penalty);
    IReadOnlyList<Penalty> GetByReservationId(Guid reservationId);
    IReadOnlyList<Penalty> GetByUserId(Guid userId);
}
