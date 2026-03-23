using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LibraryApi.Tests;

// ── CheckoutsController ────────────────────────────────────────────────────────

public class CheckoutsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CheckoutsControllerTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task GetAllCheckouts_ReturnsOk()
    {
        var response = await _client.GetAsync("/checkouts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllCheckouts_ContainsSeedCheckout()
    {
        var checkouts = await _client.GetFromJsonAsync<List<CheckoutSummaryDTO>>("/checkouts");
        checkouts.Should().NotBeNullOrEmpty();
        checkouts!.Should().ContainSingle(c => c.BookISBN == "978-0-7432-7356-5"); // Alice's 1984
    }

    [Fact]
    public async Task GetAllCheckouts_ActiveOnly_FiltersCorrectly()
    {
        var all    = await _client.GetFromJsonAsync<List<CheckoutSummaryDTO>>("/checkouts");
        var active = await _client.GetFromJsonAsync<List<CheckoutSummaryDTO>>("/checkouts?activeOnly=true");

        active.Should().OnlyContain(c => c.Status == "active");
        active!.Count.Should().BeLessThanOrEqualTo(all!.Count);
    }

    [Fact]
    public async Task GetCheckoutById_ValidId_ReturnsCheckout()
    {
        // Get any checkout id from the list first
        var all = await _client.GetFromJsonAsync<List<CheckoutSummaryDTO>>("/checkouts");
        var id  = all!.First().Id;

        var response = await _client.GetAsync($"/checkouts/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkout = await response.Content.ReadFromJsonAsync<CheckoutSummaryDTO>();
        checkout!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetCheckoutById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/checkouts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckoutRecord_IncludesMemberAndBookNames()
    {
        var checkouts = await _client.GetFromJsonAsync<List<CheckoutSummaryDTO>>("/checkouts");
        var alice1984 = checkouts!.First(c => c.BookISBN == "978-0-7432-7356-5");

        alice1984.MemberName.Should().Be("Alice Johnson");
        alice1984.BookTitle.Should().Be("1984");
        alice1984.Status.Should().Be("active");
    }
}

// ── Validation tests ───────────────────────────────────────────────────────────

public class ValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ValidationTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task CreateBook_MissingTitle_Returns400()
    {
        var payload = new { ISBN = "978-0-00-000001-1", Author = "Author", PublishedYear = 2024, Genre = "Fiction" };
        var response = await _client.PostAsJsonAsync("/books", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_InvalidISBNTooShort_Returns400()
    {
        var payload = new { ISBN = "123", Title = "T", Author = "A", PublishedYear = 2024, Genre = "G" };
        var response = await _client.PostAsJsonAsync("/books", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_PublishedYearOutOfRange_Returns400()
    {
        var payload = new { ISBN = "978-0-00-000001-1", Title = "T", Author = "A", PublishedYear = 500, Genre = "G" };
        var response = await _client.PostAsJsonAsync("/books", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMember_InvalidEmail_Returns400()
    {
        var payload = new { FullName = "Test User", Email = "not-an-email" };
        var response = await _client.PostAsJsonAsync("/members", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMember_MissingFullName_Returns400()
    {
        var payload = new { Email = "valid@example.com" };
        var response = await _client.PostAsJsonAsync("/members", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HireBook_MissingISBN_Returns400()
    {
        var memberId = Guid.Parse("22222222-0000-0000-0000-000000000002");
        var payload  = new { };
        var response = await _client.PostAsJsonAsync($"/members/{memberId}/books/hire", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

// ── Health check ───────────────────────────────────────────────────────────────

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public HealthCheckTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// Helper record matching CheckoutsController's response shape
public record CheckoutSummaryDTO(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string BookISBN,
    string BookTitle,
    DateTime CheckedOutAt,
    DateTime? ReturnedAt,
    string Status
);
