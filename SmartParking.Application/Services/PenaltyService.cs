using Microsoft.Extensions.Logging;
using SmartParking.Application.Interfaces;
using SmartParking.Infrastructure.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Application.Services;

public class PenaltyService : IPenaltyService
{
    private readonly IPenaltyRepository _penaltyRepository;
    private readonly IReservationRepository _reservationRepository;

    private readonly IWalletService _walletService;

    private readonly ILogger<PenaltyService> _logger;

    public PenaltyService(
        IPenaltyRepository penaltyRepository,
        IReservationRepository reservationRepository,
        IWalletService walletService,
        ILogger<PenaltyService> logger)
    {
        _penaltyRepository = penaltyRepository ?? throw new ArgumentNullException(nameof(penaltyRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Penalty ApplyPenalty(Guid reservationId, decimal amount, string reason)
    {
        try
        {
            _logger.LogInformation("Applying penalty. ReservationId={ReservationId}, Amount={Amount}, Reason={Reason}",
                reservationId, amount, reason);

            var reservation = _reservationRepository.GetById(reservationId);
            if (reservation == null)
            {
                _logger.LogWarning("Penalty failed - reservation not found: {ReservationId}", reservationId);
                throw new ReservationNotFoundException($"Reservation with ID {reservationId} not found");
            }

            var penalty = new Penalty(reservationId, amount, reason);
            var created = _penaltyRepository.Create(penalty);

            // Scade din wallet (cerința ta)
            _walletService.Withdraw(reservation.UserId, amount);

            _logger.LogInformation("Penalty applied successfully. PenaltyId={PenaltyId}", created.Id);
            return created;
        }
        catch (InvalidPenaltyException) { throw; }
        catch (ReservationNotFoundException) { throw; }
        catch (InsufficientBalanceException) { throw; } // din WalletService
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error applying penalty for reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public IReadOnlyList<Penalty> GetUserPenalties(Guid userId)
    {
        try
        {
            _logger.LogInformation("Getting penalties for user {UserId}", userId);
            return _penaltyRepository.GetByUserId(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting penalties for user {UserId}", userId);
            throw;
        }
    }

    public IReadOnlyList<Penalty> GetReservationPenalties(Guid reservationId)
    {
        try
        {
            _logger.LogInformation("Getting penalties for reservation {ReservationId}", reservationId);
            return _penaltyRepository.GetByReservationId(reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting penalties for reservation {ReservationId}", reservationId);
            throw;
        }
    }
}
