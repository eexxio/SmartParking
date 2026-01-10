namespace SmartParking.Business.Configuration
{
    public class NotificationSettings
    {
        public string ApiKey { get; set; } = "MOCK_KEY";
        public string FromEmail { get; set; } = "tesh.alexandru@gmail.com";
        public string ApiUrl { get; set; } = "https://api.sendgrid.com/v3/mail/send";
        public bool IsSimulationMode { get; set; } = false;
    }
}