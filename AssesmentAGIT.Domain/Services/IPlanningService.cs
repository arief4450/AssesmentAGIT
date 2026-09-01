using AssesmentAGIT.Domain.DTOs;

namespace AssesmentAGIT.Domain.Services;

public interface IPlanningService
{
    Task<PlanningResultDto> CreatePlanningAsync(CreatePlanningRequest request);
    Task<PlanningResultDto?> GetPlanningByRequestCodeAsync(string requestCode);
    Task<List<PlanningListItemDto>> GetPlanningHistoryAsync(int page, int pageSize);
}
