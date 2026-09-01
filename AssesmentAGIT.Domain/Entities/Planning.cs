namespace AssesmentAGIT.Domain.Entities;

public class Planning
{
    public Guid PlanningId { get; set; }
    public required string RequestCode { get; set; }
    public required string CandidateToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string Status { get; set; }

    public ICollection<PlanningSlot> Slots { get; set; } = new List<PlanningSlot>();
}
