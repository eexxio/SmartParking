using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SpotId { get; private set; }

    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }

    public ReservationStatus Status { get; private set; }
    public DateTime CancellationDeadline { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Reservation(Guid userId, Guid spotId)
    {
        if (userId == Guid.Empty) throw new InvalidReservationException("UserId is required.");
        if (spotId == Guid.Empty) throw new InvalidReservationException("SpotId is required.");

        Id = Guid.NewGuid();
        UserId = userId;
        SpotId = spotId;
        StartTime = DateTime.UtcNow;
        EndTime = null;
        Status = ReservationStatus.Pending;
        CancellationDeadline = DateTime.UtcNow.AddMinutes(15);
        CreatedAt = DateTime.UtcNow;
    }

    public Reservation(
        Guid id,
        Guid userId,
        Guid spotId,
        DateTime startTime,
        DateTime? endTime,
        ReservationStatus status,
        DateTime cancellationDeadline,
        DateTime createdAt)
    {
        if (userId == Guid.Empty) throw new InvalidReservationException("UserId is required.");
        if (spotId == Guid.Empty) throw new InvalidReservationException("SpotId is required.");

        if (endTime.HasValue && endTime.Value <= startTime)
            throw new InvalidReservationException("EndTime must be greater than StartTime.");

        Id = id;
        UserId = userId;
        SpotId = spotId;
        StartTime = startTime;
        EndTime = endTime;
        Status = status;
        CancellationDeadline = cancellationDeadline;
        CreatedAt = createdAt;
    }

    public Reservation()
    {

    }

    public bool CanCancel(DateTime now)
    {
        return now <= CancellationDeadline;
    }

    public decimal CalculateDurationHours()
    {
        if (!EndTime.HasValue)
            throw new InvalidReservationException("Cannot calculate duration because EndTime is not set.");

        var hours = (EndTime.Value - StartTime).TotalHours;
        if (hours < 0) throw new InvalidReservationException("Invalid duration.");

        return (decimal)hours;
    }
}
