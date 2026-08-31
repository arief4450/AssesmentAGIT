using System;
using System.Linq;

namespace AssesmentAGIT.Domain;

public class BalancingResult
{
    public required decimal[] BalancedValues { get; init; }
}

public static class PlanningBalancer
{
    public static BalancingResult Balance(decimal[] originalValues)
    {
        if (originalValues == null || originalValues.Length == 0)
            throw new ArgumentException("Input must not be null or empty.");

        foreach (var v in originalValues)
        {
            if (v < 0)
                throw new ArgumentException("Negative values are not allowed.");
            
            if (v % 1 != 0)
                throw new ArgumentException("Fractional values are not allowed.");
        }

        var result = new decimal[originalValues.Length];

        var activeIndices = Enumerable.Range(0, originalValues.Length)
            .Where(i => originalValues[i] > 0)
            .ToList();

        if (activeIndices.Count == 0)
        {
            return new BalancingResult { BalancedValues = result };
        }

        decimal total = activeIndices.Sum(i => originalValues[i]);
        int n = activeIndices.Count;
        
        decimal baseValue = Math.Floor(total / n);
        int remainder = (int)(total % n);

        foreach (var i in activeIndices)
            result[i] = baseValue;

        var priorityOrder = activeIndices
            .OrderByDescending(i => originalValues[i])
            .ThenBy(i => i)
            .Take(remainder);

        foreach (var i in priorityOrder)
            result[i] += 1m;

        return new BalancingResult { BalancedValues = result };
    }
}
