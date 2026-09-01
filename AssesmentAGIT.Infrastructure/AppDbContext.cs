using AssesmentAGIT.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssesmentAGIT.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Planning> Plannings => Set<Planning>();
    public DbSet<PlanningSlot> PlanningSlots => Set<PlanningSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Planning>(entity =>
        {
            entity.HasKey(e => e.PlanningId);

            entity.Property(e => e.PlanningId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.RequestCode)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.RequestCode)
                .IsUnique();

            entity.Property(e => e.CandidateToken)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasMany(e => e.Slots)
                .WithOne(s => s.Planning)
                .HasForeignKey(s => s.PlanningId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanningSlot>(entity =>
        {
            entity.HasKey(e => e.PlanningSlotId);

            entity.Property(e => e.PlanningSlotId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.SlotName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.OriginalQuantity)
                .HasPrecision(18, 2);

            entity.Property(e => e.BalancedQuantity)
                .HasPrecision(18, 2);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_PlanningSlot_OriginalQuantity_NonNegative",
                    "\"OriginalQuantity\" >= 0");
                t.HasCheckConstraint("CK_PlanningSlot_BalancedQuantity_NonNegative",
                    "\"BalancedQuantity\" >= 0");
            });
        });
    }
}
