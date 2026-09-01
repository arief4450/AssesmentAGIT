namespace AssesmentAGIT.Domain.Entities;

public class PlanningSlot
{
    public Guid PlanningSlotId { get; set; }
    public Guid PlanningId { get; set; }
    public int SlotOrder { get; set; }
    public required string SlotName { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal BalancedQuantity { get; set; }
    public bool IsActive { get; set; }

    public Planning? Planning { get; set; }
}
