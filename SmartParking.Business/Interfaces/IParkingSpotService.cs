using System;
using SmartParking.Domain.Entities;

namespace SmartParking.Business.Interfaces
{
    public interface IParkingSpotService
    {
        ParkingSpot GetSpot(Guid id);

    }
}