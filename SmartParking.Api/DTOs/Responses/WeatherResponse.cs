namespace SmartParking.Api.DTOs.Responses;

public record WeatherResponse(
    string Location,
    double Temperature,
    string Description,
    double Humidity,
    double WindSpeed
);
