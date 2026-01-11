using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartParking.Application.Configuration;
using SmartParking.Application.Interfaces;

namespace SmartParking.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly NotificationSettings _settings;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(HttpClient httpClient, NotificationSettings settings, ILogger<NotificationService> logger)
        {
            _httpClient = httpClient;
            _settings = settings;
            _logger = logger;
        }

        public async Task SendPaymentConfirmationAsync(string email, decimal amount)
        {
            await SendEmailWithRetryAsync(email, "Payment Successful", $"We received {amount} RON.");
        }

        public async Task SendReservationConfirmationAsync(string email, string spotNumber)
        {
            await SendEmailWithRetryAsync(email, "Reservation Confirmed", $"Spot {spotNumber} is yours.");
        }

        private async Task SendEmailWithRetryAsync(string toEmail, string subject, string body)
        {
            if (_settings.IsSimulationMode)
            {
                _logger.LogInformation($"[SIMULATION] Email to {toEmail}: {subject}");
                return;
            }

            var emailPayload = new
            {
                personalizations = new[] { new { to = new[] { new { email = toEmail } } } },
                from = new { email = _settings.FromEmail },
                subject = subject,
                content = new[] { new { type = "text/plain", value = body } }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(emailPayload),
                Encoding.UTF8,
                "application/json");

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            }

            int maxRetries = 3;
            int currentRetry = 0;
            bool sent = false;

            while (currentRetry < maxRetries && !sent)
            {
                currentRetry++;
                try
                {
                    var response = await _httpClient.PostAsync(_settings.ApiUrl, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        sent = true;
                        _logger.LogInformation($"Email sent successfully to {toEmail}");
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Attempt {currentRetry} failed. Status: {response.StatusCode}. Details: {error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Attempt {currentRetry} threw exception: {ex.Message}");
                }

                if (!sent && currentRetry < maxRetries)
                {
                    await Task.Delay(500);
                }
            }

            if (!sent)
            {
                throw new HttpRequestException($"Failed to send email to {toEmail} after {maxRetries} attempts.");
            }
        }
    }
}