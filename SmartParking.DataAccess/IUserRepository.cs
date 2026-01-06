using SmartParking.Domain;

namespace SmartParking.DataAccess;

public interface IUserRepository
{
    User Create(User user);
    User? GetById(Guid id);
    User? GetByEmail(string email);
    void Update(User user);
    void Delete(Guid id);
}
