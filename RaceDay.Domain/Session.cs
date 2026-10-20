namespace RaceDay.Domain;

public class Session
{
    public int Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }

    public UserRole Role { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
