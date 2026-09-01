using System.Net;
using System.Net.Http.Json;
using AssesmentAGIT.Domain.DTOs;
using AssesmentAGIT.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssesmentAGIT.Tests;

public class PlanningApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private const string TestDbName = "IntegrationTestDb";

    public PlanningApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var descriptorsToRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                        d.ServiceType.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var d in descriptorsToRemove)
                    services.Remove(d);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(TestDbName));
            });
        });
        using var scope = appFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        _client = appFactory.CreateClient();
    }

    private static CreatePlanningRequest BuildValidRequest(string requestCode) => new()
    {
        RequestCode = requestCode,
        Slots = new List<SlotInputDto>
        {
            new() { SlotName = "Monday",    Quantity = 4 },
            new() { SlotName = "Tuesday",   Quantity = 5 },
            new() { SlotName = "Wednesday", Quantity = 1 },
            new() { SlotName = "Thursday",  Quantity = 7 },
            new() { SlotName = "Friday",    Quantity = 6 },
            new() { SlotName = "Saturday",  Quantity = 4 },
            new() { SlotName = "Sunday",    Quantity = 0 },
        }
    };

    [Fact]
    public async Task PostPlanning_ValidRequest_Returns200WithBalancedResult()
    {  
        var request = BuildValidRequest("INT-TEST-001");
        var response = await _client.PostAsJsonAsync("/api/planning", request);
        var result = await response.Content.ReadFromJsonAsync<PlanningResultDto>();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.RequestCode.Should().Be("INT-TEST-001");
        result.Status.Should().Be("Success");
        result.IsTotalValid.Should().BeTrue("Balanced total must equal original total");
        result.OriginalTotal.Should().Be(27);
        result.BalancedTotal.Should().Be(27);
        result.Slots.Should().HaveCount(7);
        result.Slots.Last().BalancedQuantity.Should().Be(0, "Inactive slot must remain 0");
    }

    [Fact]
    public async Task PostPlanning_SameRequestCodeTwice_DoesNotCreateDuplicate()
    {
        var request = BuildValidRequest("INT-TEST-IDEMPOTENT");

        var response1 = await _client.PostAsJsonAsync("/api/planning", request);
        var response2 = await _client.PostAsJsonAsync("/api/planning", request);

        var result1 = await response1.Content.ReadFromJsonAsync<PlanningResultDto>();
        var result2 = await response2.Content.ReadFromJsonAsync<PlanningResultDto>();

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        result1!.PlanningId.Should().Be(result2!.PlanningId,
            "Resubmitting the same RequestCode must return the existing record, not a new one");
    }

    [Fact]
    public async Task PostPlanning_InvalidInput_NegativeQuantity_Returns400()
    {
        var request = new CreatePlanningRequest
        {
            RequestCode = "INT-TEST-NEGATIVE",
            Slots = new List<SlotInputDto>
            {
                new() { SlotName = "Monday", Quantity = -1 },
                new() { SlotName = "Tuesday", Quantity = 5 },
                new() { SlotName = "Wednesday", Quantity = 3 },
                new() { SlotName = "Thursday", Quantity = 3 },
                new() { SlotName = "Friday", Quantity = 3 },
                new() { SlotName = "Saturday", Quantity = 3 },
                new() { SlotName = "Sunday", Quantity = 0 },
            }
        };

        var response = await _client.PostAsJsonAsync("/api/planning", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPlanning_ExistingRequestCode_Returns200WithDetail()
    {  
        var request = BuildValidRequest("INT-TEST-GET");
        await _client.PostAsJsonAsync("/api/planning", request);

        var response = await _client.GetAsync("/api/planning/INT-TEST-GET");
        var result = await response.Content.ReadFromJsonAsync<PlanningResultDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.RequestCode.Should().Be("INT-TEST-GET");
        result.Slots.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetPlanning_NonExistentRequestCode_Returns404()
    {
        var response = await _client.GetAsync("/api/planning/DOES-NOT-EXIST");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPlanningHistory_AfterSubmissions_ReturnsNewestFirst()
    {
        await _client.PostAsJsonAsync("/api/planning", BuildValidRequest("INT-HIST-A"));
        await Task.Delay(10);
        await _client.PostAsJsonAsync("/api/planning", BuildValidRequest("INT-HIST-B"));
        var response = await _client.GetAsync("/api/planning?page=1&pageSize=10");
        var result = await response.Content.ReadFromJsonAsync<List<PlanningListItemDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        result.First().CreatedAt.Should().BeOnOrAfter(result.Last().CreatedAt,
            "History must be ordered newest-first");
    }
}
