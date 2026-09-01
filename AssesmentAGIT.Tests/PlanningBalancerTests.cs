using System;
using System.Linq;
using AssesmentAGIT.Domain;
using FluentAssertions;
using Xunit;

namespace AssesmentAGIT.Tests;

public class PlanningBalancerTests
{
    private static void AssertInvariant(decimal[] original, decimal[] balanced)
    {
        balanced.Sum().Should().Be(original.Sum(), "Total must remain exactly the same");

        for (int i = 0; i < original.Length; i++)
        {
            if (original[i] == 0)
            {
                balanced[i].Should().Be(0, $"Slot {i} was inactive and should remain 0");
            }
        }

        var activeBalanced = balanced.Where((v, index) => original[index] > 0).ToList();
        if (activeBalanced.Count > 0)
        {
            var maxDiff = activeBalanced.Max() - activeBalanced.Min();
            maxDiff.Should().BeLessThanOrEqualTo(1, "The difference between max and min of active slots must be <= 1");
        }
    }

    [Fact]
    public void Balance_SampleCase_ReturnsExpectedOutput()
    {
        decimal[] input = [4, 5, 1, 7, 6, 4, 0];
        decimal[] expected = [4, 5, 4, 5, 5, 4, 0];

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }

    [Fact]
    public void Balance_EvenlyDivisibleTotal_DistributesEqually()
    {
        decimal[] input = [3, 3, 3];
        decimal[] expected = [3, 3, 3];

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }

    [Fact]
    public void Balance_TotalWithRemainder_DistributesToHighestInitialValue()
    {
        decimal[] input = [1, 1, 3]; 
        decimal[] expected = [2, 1, 2];

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }

    [Fact]
    public void Balance_AllZeros_ReturnsAllZeros()
    {
        decimal[] input = [0, 0, 0, 0];
        decimal[] expected = [0, 0, 0, 0];

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }

    [Fact]
    public void Balance_SingleActiveSlot_ReturnsUnchanged()
    {
        decimal[] input = [0, 10, 0];
        decimal[] expected = [0, 10, 0];

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }

    [Fact]
    public void Balance_TieBreaker_PrioritizesEarlierSlot()
    {
        decimal[] input = [6, 6, 4];
        decimal[] expected = [6, 5, 5];

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }

    [Fact]
    public void Balance_InvalidInput_ThrowsArgumentException()
    {
        Action actNegative = () => PlanningBalancer.Balance([-1, 5, 5]);
        actNegative.Should().Throw<ArgumentException>().WithMessage("*Negative*");

        Action actFractional = () => PlanningBalancer.Balance([1.5m, 2m]);
        actFractional.Should().Throw<ArgumentException>().WithMessage("*Fractional*");
    }

    [Fact]
    public void Balance_EmptyOrNullInput_ThrowsArgumentException()
    {
        Action actEmpty = () => PlanningBalancer.Balance([]);
        actEmpty.Should().Throw<ArgumentException>().WithMessage("*empty*");

        Action actNull = () => PlanningBalancer.Balance(null!);
        actNull.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    [Fact]
    public void Balance_ExtremeValues_BalancesCorrectly()
    {
        decimal[] input = [1000000m, 1000002m];
        decimal[] expected = [1000001m, 1000001m];

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }

    [Fact]
    public void Balance_LargeNumberOfSlots_BalancesCorrectly()
    {
        var input = new decimal[30];
        var expected = new decimal[30];
        
        for (int i = 0; i < 30; i++)
        {
            if (i % 2 == 0)
            {
                input[i] = 3m;
                expected[i] = 3m;
            }
        }

        var result = PlanningBalancer.Balance(input);

        result.BalancedValues.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        AssertInvariant(input, result.BalancedValues);
    }
}
