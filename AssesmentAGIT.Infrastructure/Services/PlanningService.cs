using AssesmentAGIT.Domain.DTOs;
using AssesmentAGIT.Domain.Entities;
using AssesmentAGIT.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace AssesmentAGIT.Infrastructure.Services;

public class PlanningService : IPlanningService
{
    private readonly AppDbContext _db;
    private const string CandidateToken = "VEH-Arief_Achmadi";

    public PlanningService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PlanningResultDto> CreatePlanningAsync(CreatePlanningRequest request)
    {
        // Idempotency: return existing record if RequestCode already exists
        var existing = await _db.Plannings
            .Include(p => p.Slots)
            .FirstOrDefaultAsync(p => p.RequestCode == request.RequestCode);

        if (existing != null)
            return MapToResult(existing);

        // Validate input
        var quantities = request.Slots.Select(s => s.Quantity).ToArray();
        var balanced = Domain.PlanningBalancer.Balance(quantities);

        // Build entities
        var planning = new Planning
        {
            PlanningId = Guid.NewGuid(),
            RequestCode = request.RequestCode,
            CandidateToken = CandidateToken,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = "Success"
        };

        var slots = request.Slots.Select((slot, index) => new PlanningSlot
        {
            PlanningSlotId = Guid.NewGuid(),
            PlanningId = planning.PlanningId,
            SlotOrder = index + 1,
            SlotName = slot.SlotName,
            OriginalQuantity = slot.Quantity,
            BalancedQuantity = balanced.BalancedValues[index],
            IsActive = slot.Quantity > 0
        }).ToList();

        planning.Slots = slots;

        // Atomic save — wrap in a transaction if the provider supports it (PostgreSQL does; InMemory does not)
        var supportsTransactions = _db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        if (supportsTransactions)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Plannings.Add(planning);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            _db.Plannings.Add(planning);
            await _db.SaveChangesAsync();
        }

        return MapToResult(planning);
    }

    public async Task<PlanningResultDto?> GetPlanningByRequestCodeAsync(string requestCode)
    {
        var planning = await _db.Plannings
            .Include(p => p.Slots.OrderBy(s => s.SlotOrder))
            .FirstOrDefaultAsync(p => p.RequestCode == requestCode);

        return planning == null ? null : MapToResult(planning);
    }

    public async Task<List<PlanningListItemDto>> GetPlanningHistoryAsync(int page, int pageSize)
    {
        return await _db.Plannings
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PlanningListItemDto
            {
                PlanningId = p.PlanningId,
                RequestCode = p.RequestCode,
                CreatedAt = p.CreatedAt,
                Status = p.Status,
                ActiveSlotCount = p.Slots.Count(s => s.IsActive),
                OriginalTotal = p.Slots.Sum(s => s.OriginalQuantity),
                BalancedTotal = p.Slots.Sum(s => s.BalancedQuantity)
            })
            .ToListAsync();
    }

    private static PlanningResultDto MapToResult(Planning planning)
    {
        var originalTotal = planning.Slots.Sum(s => s.OriginalQuantity);
        var balancedTotal = planning.Slots.Sum(s => s.BalancedQuantity);

        return new PlanningResultDto
        {
            PlanningId = planning.PlanningId,
            RequestCode = planning.RequestCode,
            CandidateToken = planning.CandidateToken,
            CreatedAt = planning.CreatedAt,
            Status = planning.Status,
            OriginalTotal = originalTotal,
            BalancedTotal = balancedTotal,
            IsTotalValid = originalTotal == balancedTotal,
            Slots = planning.Slots
                .OrderBy(s => s.SlotOrder)
                .Select(s => new SlotResultDto
                {
                    SlotOrder = s.SlotOrder,
                    SlotName = s.SlotName,
                    OriginalQuantity = s.OriginalQuantity,
                    BalancedQuantity = s.BalancedQuantity,
                    IsActive = s.IsActive
                }).ToList()
        };
    }
}
