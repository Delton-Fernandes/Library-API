using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryApi.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LibraryApi.Tests;

public class CheckoutsAdminControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CheckoutsAdminControllerTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsAllCheckoutsIncludingSeedData()
    {
        var checkouts = await _client.GetFromJsonAsync<List<CheckoutSummaryDto>>("/checkouts");
        checkouts.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAll_ActiveOnly_OnlyReturnsActiveCheckouts()
    {
        var checkouts = await _client.GetFromJsonAsync<List<CheckoutSummaryDto>>("/checkouts?activeOnly=true");
        checkouts.Should().AllSatisfy(c => c.Status.Should().Be("active"));
    }

    [Fact]
    public async Task GetAll_SeedData_HasActiveAliceCheckout()
    {
        var checkouts = await _client.GetFromJsonAsync<List<CheckoutSummaryDto>>("/checkouts?activeOnly=true");
        checkouts.Should().Contain(c => c.MemberName == "Alice Johnson" && c.BookISBN == "978-0-7432-7356-5");
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/checkouts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_AfterHire_ReturnsCheckout()
    {
        // Bob hires a book so we have a fresh checkout ID to look up
        var bobId    = Guid.Parse("22222222-0000-0000-0000-000000000002");
        var hireResp = await _client.PostAsJsonAsync(
            $"/members/{bobId}/books/hire",
            new { bookISBN = "978-0-14-028329-7" });

        hireResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var checkout = await hireResp.Content.ReadFromJsonAsync<CheckoutSummaryDto>();

        var getResp = await _client.GetAsync($"/checkouts/{checkout!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResp.Content.ReadFromJsonAsync<CheckoutSummaryDto>();
        fetched!.MemberName.Should().Be("Bob Smith");
        fetched.Status.Should().Be("active");
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
