using System.ComponentModel.DataAnnotations;

namespace RaceDay.Domain;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }

    [Required]
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    // Navigation property
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
