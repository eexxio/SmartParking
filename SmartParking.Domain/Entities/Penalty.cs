using SmartParking.Domain.Exceptions;

namespace SmartParking.Domain.Entities;

public class Penalty
{
    public Guid Id { get; private set; }
    public Guid ReservationId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Penalty(Guid reservationId, decimal amount, string reason)
    {
        Validate(reservationId, amount, reason);

        Id = Guid.NewGuid();
        ReservationId = reservationId;
        Amount = amount;
        Reason = reason;
        CreatedAt = DateTime.UtcNow;
    }

    public Penalty(Guid id, Guid reservationId, decimal amount, string reason, DateTime createdAt)
    {
        Validate(reservationId, amount, reason);

        Id = id;
        ReservationId = reservationId;
        Amount = amount;
        Reason = reason;
        CreatedAt = createdAt;
    }

    private static void Validate(Guid reservationId, decimal amount, string reason)
    {
        if (reservationId == Guid.Empty)
            throw new InvalidPenaltyException("ReservationId is required.");

        if (amount <= 0)
            throw new InvalidPenaltyException("Amount must be greater than 0.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5)
            throw new InvalidPenaltyException("Reason must be at least 5 characters long.");
    }
}
