using SmartParking.Domain;

namespace SmartParking.Business;

public interface IUserService
{
    User RegisterUser(string email, string fullName, bool isEVUser);
    User GetUser(Guid userId);
    void UpdateUser(Guid userId, string fullName, bool isEVUser);
}
