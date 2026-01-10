using Microsoft.Extensions.Logging;
using SmartParking.Business.Interfaces;
using SmartParking.DataAccess.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Business.Services;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly SmartParking.Business.IUserService _userService;
    private readonly IPenaltyService _penaltyService;
    private readonly ILogger<ReservationService> _logger;

    private const decimal LateCancellationPenaltyAmount = 10.00m;

    public ReservationService(
        IReservationRepository reservationRepository,
        SmartParking.Business.IUserService userService,
        IPenaltyService penaltyService,
        ILogger<ReservationService> logger)
    {
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _penaltyService = penaltyService ?? throw new ArgumentNullException(nameof(penaltyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Reservation CreateReservation(Guid userId, Guid spotId, int cancellationTimeoutMinutes = 15)
    {
        try
        {
            _logger.LogInformation("Creating reservation. UserId={UserId}, SpotId={SpotId}", userId, spotId);

            var user = _userService.GetUser(userId); // existent la Member1 

            // Validare spot direct prin SQL (fără Member2 code)
            _reservationRepository.ValidateSpotForUser(spotId, user.IsEVUser);

            var reservation = _reservationRepository.Create(userId, spotId, cancellationTimeoutMinutes);

            _logger.LogInformation("Reservation created successfully. ReservationId={ReservationId}", reservation.Id);
            return reservation;
        }
        catch (InvalidReservationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation for user {UserId} on spot {SpotId}", userId, spotId);
            throw;
        }
    }

    public void ConfirmReservation(Guid reservationId)
    {
        try
        {
            _logger.LogInformation("Confirming reservation {ReservationId}", reservationId);
            _reservationRepository.Confirm(reservationId);
            _logger.LogInformation("Reservation confirmed {ReservationId}", reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public void CancelReservation(Guid reservationId)
    {
        try
        {
            _logger.LogInformation("Cancelling reservation {ReservationId}", reservationId);

            var isLate = _reservationRepository.Cancel(reservationId);

            if (isLate)
            {
                _penaltyService.ApplyPenalty(reservationId, LateCancellationPenaltyAmount, "Late cancellation");
                _logger.LogInformation("Late cancellation penalty applied for reservation {ReservationId}", reservationId);
            }

            _logger.LogInformation("Reservation cancelled {ReservationId}", reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public void CompleteReservation(Guid reservationId)
    {
        try
        {
            _logger.LogInformation("Completing reservation {ReservationId}", reservationId);

            _reservationRepository.Complete(reservationId);

            _logger.LogInformation("Reservation completed {ReservationId}", reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public IReadOnlyList<Reservation> GetUserReservations(Guid userId)
    {
        try
        {
            _logger.LogInformation("Getting reservations for user {UserId}", userId);
            return _reservationRepository.GetByUserId(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservations for user {UserId}", userId);
            throw;
        }
    }

    public int CheckAndApplyTimeoutPenalties()
    {
        try
        {
            _logger.LogInformation("Checking expired pending reservations...");

            var expired = _reservationRepository.GetExpiredPendingReservations();
            var processed = 0;

            foreach (var r in expired)
            {
                var isLate = _reservationRepository.Cancel(r.Id);

                if (isLate)
                {
                    _penaltyService.ApplyPenalty(r.Id, LateCancellationPenaltyAmount, "Reservation timeout");
                }

                processed++;
            }

            _logger.LogInformation("Timeout penalties processed: {Count}", processed);
            return processed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying timeout penalties");
            throw;
        }
    }


    public Reservation GetReservation(Guid reservationId)
    {
        try
        {
            var reservation = _reservationRepository.GetById(reservationId);
            if (reservation is null)
            {
                throw new InvalidReservationException($"Reservation with ID {reservationId} not found.");
            }
            return reservation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation {ReservationId}", reservationId);
            throw;
        }
    }
}
