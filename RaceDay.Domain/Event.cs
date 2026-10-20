using System.ComponentModel.DataAnnotations;

namespace RaceDay.Domain;

public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public decimal DistanceKm { get; set; }

    [Required]
    public EventType EventType { get; set; }

    public string? BannerImageUrl { get; set; }

    // Foreign Key linking to the Organiser (User)
    [Required]
    public int OrganiserId { get; set; }
    public User Organiser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
