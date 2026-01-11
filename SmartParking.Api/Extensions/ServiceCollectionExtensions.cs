using SmartParking.Application.Configuration;
using SmartParking.Application.Interfaces;
using SmartParking.Application.Services;
using SmartParking.Infrastructure.Interfaces;
using SmartParking.Infrastructure.Repositories;

namespace SmartParking.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured");

        // Register Repositories (Scoped - per request)
        services.AddScoped<IUserRepository>(sp =>
            new UserRepository(connectionString));

        services.AddScoped<IWalletRepository>(sp =>
            new WalletRepository(connectionString));

        services.AddScoped<IParkingSpotRepository>(sp =>
            new ParkingSpotRepository(connectionString));

        services.AddScoped<IReservationRepository>(sp =>
            new ReservationRepository(connectionString));

        // PaymentRepository has different constructor - takes IConfiguration
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddScoped<IPenaltyRepository>(sp =>
            new PenaltyRepository(connectionString));

        // Register Services (Scoped - per request)
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IParkingSpotService, ParkingSpotService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPenaltyService, PenaltyService>();

        // Register NotificationService with configuration
        var notificationSettings = configuration
            .GetSection("NotificationSettings")
            .Get<NotificationSettings>()
            ?? new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                FromEmail = "tesh.alexandru@gmail.com",
                ApiUrl = "https://api.sendgrid.com/v3/mail/send",
                IsSimulationMode = true
            };

        services.AddSingleton(notificationSettings);
        services.AddHttpClient<INotificationService, NotificationService>();

        // Register WeatherService with HttpClient
        services.AddHttpClient<IWeatherService, WeatherService>();

        return services;
    }
}
