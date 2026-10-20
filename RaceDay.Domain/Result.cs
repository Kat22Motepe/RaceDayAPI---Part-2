namespace RaceDay.Domain;

public class Result
{
    public int Id { get; set; }

    // Foreign key to Enrolment
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;

    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
