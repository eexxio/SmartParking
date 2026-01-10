using System;

namespace SmartParking.Domain.Entities
{
    public class ParkingSpot
    {
        public Guid Id { get; set; }
        public string SpotNumber { get; set; }
        public string SpotType { get; set; }
        public decimal HourlyRate { get; set; }
        public bool IsOccupied { get; set; }
    }
}