namespace AssesmentAGIT.Domain.DTOs;

public class SlotInputDto
{
    public string SlotName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class CreatePlanningRequest
{
    public string RequestCode { get; set; } = string.Empty;
    public List<SlotInputDto> Slots { get; set; } = new();
}

public class SlotResultDto
{
    public int SlotOrder { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public decimal OriginalQuantity { get; set; }
    public decimal BalancedQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class PlanningResultDto
{
    public Guid PlanningId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string CandidateToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal OriginalTotal { get; set; }
    public decimal BalancedTotal { get; set; }
    public bool IsTotalValid { get; set; }
    public List<SlotResultDto> Slots { get; set; } = new();
}

public class PlanningListItemDto
{
    public Guid PlanningId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ActiveSlotCount { get; set; }
    public decimal OriginalTotal { get; set; }
    public decimal BalancedTotal { get; set; }
}
