using System.Text.RegularExpressions;

namespace SmartParking.Domain;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string FullName { get; private set; }
    public bool IsEVUser { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public User(string email, string fullName, bool isEVUser)
    {
        ValidateEmail(email);
        ValidateFullName(fullName);

        Id = Guid.NewGuid();
        Email = email;
        FullName = fullName;
        IsEVUser = isEVUser;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public User(Guid id, string email, string fullName, bool isEVUser, DateTime createdAt, bool isActive)
    {
        ValidateEmail(email);
        ValidateFullName(fullName);

        Id = id;
        Email = email;
        FullName = fullName;
        IsEVUser = isEVUser;
        CreatedAt = createdAt;
        IsActive = isActive;
    }

    public void Update(string fullName, bool isEVUser)
    {
        ValidateFullName(fullName);
        FullName = fullName;
        IsEVUser = isEVUser;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentNullException(nameof(email), "Email is required");
        }

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        if (!emailRegex.IsMatch(email))
        {
            throw new ArgumentException("Invalid email format", nameof(email));
        }
    }

    private static void ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentNullException(nameof(fullName), "Full name is required");
        }

        if (fullName.Length < 5)
        {
            throw new ArgumentException("Full name must be at least 5 characters long", nameof(fullName));
        }
    }
}
