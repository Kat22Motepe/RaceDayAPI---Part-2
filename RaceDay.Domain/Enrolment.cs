namespace RaceDay.Domain;

public class Enrolment
{
    public int Id { get; set; }

    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Confirmed;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // One enrolment has one result (nullable because they haven't finished yet)
    public Result? Result { get; set; }
}
