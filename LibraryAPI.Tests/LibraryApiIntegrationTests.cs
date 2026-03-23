using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryAPI.Models;
using LibraryAPI.ModelsDTO;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LibraryApi.Tests;

public class BooksControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BooksControllerTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task GetAllBooks_ReturnsOk_WithBooks()
    {
        var response = await _client.GetAsync("/books");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var books = await response.Content.ReadFromJsonAsync<List<BookDTO>>();
        books.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAllBooks_SeedData_HasCheckedOutBook()
    {
        var books = await _client.GetFromJsonAsync<List<BookDTO>>("/books");
        books.Should().Contain(b => b.Status == "checked out");
        books.Should().Contain(b => b.Status == "available");
    }

    [Fact]
    public async Task GetBookByISBN_ExistingBook_ReturnsBook()
    {
        var response = await _client.GetAsync("/books/978-0-06-112008-4");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var book = await response.Content.ReadFromJsonAsync<BookDTO>();
        book!.Title.Should().Be("To Kill a Mockingbird");
        book.Status.Should().Be("available");
    }

    [Fact]
    public async Task GetBookByISBN_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/books/999-does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBook_ValidRequest_Returns201WithBook()
    {
        var request = new CreateBookRequest(
            "978-0-00-000001-1",
            "Test Book",
            "Test Author",
            2024,
            "Test Genre"
        );

        var response = await _client.PostAsJsonAsync("/books", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var book = await response.Content.ReadFromJsonAsync<BookDTO>();
        book!.ISBN.Should().Be(request.ISBN);
        book.Status.Should().Be("available");
    }

    [Fact]
    public async Task CreateBook_DuplicateISBN_Returns409()
    {
        var request = new CreateBookRequest(
            "978-0-06-112008-4", // already seeded
            "Duplicate", "Duplicate Author", 2024, "Fiction"
        );

        var response = await _client.PostAsJsonAsync("/books", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

public class MembersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MembersControllerTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task GetAllMembers_ReturnsOkWithMembers()
    {
        var response = await _client.GetAsync("/members");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var members = await response.Content.ReadFromJsonAsync<List<MemberDTO>>();
        members.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetMember_ExistingId_ReturnsMember()
    {
        var id = Guid.Parse("11111111-0000-0000-0000-000000000001");
        var response = await _client.GetAsync($"/members/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var member = await response.Content.ReadFromJsonAsync<MemberDTO>();
        member!.FullName.Should().Be("Alice Johnson");
    }

    [Fact]
    public async Task GetMember_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/members/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMember_ValidRequest_Returns201()
    {
        var request = new CreateMemberRequest("New Member", "new@example.com");
        var response = await _client.PostAsJsonAsync("/members", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var member = await response.Content.ReadFromJsonAsync<MemberDTO>();
        member!.FullName.Should().Be("New Member");
        member.Id.Should().NotBeEmpty();
    }
}

public class CheckoutControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CheckoutControllerTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    private static readonly Guid AliceId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid BobId   = Guid.Parse("22222222-0000-0000-0000-000000000002");

    [Fact]
    public async Task GetCheckedOut_Alice_ReturnsHer1984()
    {
        var checkouts = await _client.GetFromJsonAsync<List<CheckoutDTO>>($"/members/{AliceId}/books/checkedout");
        checkouts.Should().ContainSingle(c => c.BookISBN == "978-0-7432-7356-5");
    }

    [Fact]
    public async Task GetCheckedOut_UnknownMember_Returns404()
    {
        var response = await _client.GetAsync($"/members/{Guid.NewGuid()}/books/checkedout");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HireBook_Available_Returns201AndBookBecomesCheckedOut()
    {
        // Bob hires "To Kill a Mockingbird" (available in seed)
        var hireReq = new HireBookRequest("978-0-06-112008-4");
        var hireResp = await _client.PostAsJsonAsync($"/members/{BobId}/books/hire", hireReq);
        hireResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var checkout = await hireResp.Content.ReadFromJsonAsync<CheckoutDTO>();
        checkout!.MemberId.Should().Be(BobId);
        checkout.ReturnedAt.Should().BeNull();

        // Book should now show as checked out
        var book = await _client.GetFromJsonAsync<BookDTO>("/books/978-0-06-112008-4");
        book!.Status.Should().Be("checked out");
    }

    [Fact]
    public async Task HireBook_AlreadyCheckedOut_Returns409()
    {
        // 1984 is already checked out by Alice in seed data
        var hireReq = new HireBookRequest("978-0-7432-7356-5");
        var response = await _client.PostAsJsonAsync($"/members/{BobId}/books/hire", hireReq);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task HireBook_UnknownMember_Returns404()
    {
        var hireReq = new HireBookRequest("978-0-06-112008-4");
        var response = await _client.PostAsJsonAsync($"/members/{Guid.NewGuid()}/books/hire", hireReq);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HireBook_UnknownISBN_Returns404()
    {
        var hireReq = new HireBookRequest("000-not-a-book");
        var response = await _client.PostAsJsonAsync($"/members/{BobId}/books/hire", hireReq);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnBook_ActiveCheckout_Returns200AndBookBecomesAvailable()
    {
        // Alice returns 1984 (checked out in seed)
        var returnReq = new HireBookRequest("978-0-7432-7356-5");
        var response = await _client.PostAsJsonAsync($"/members/{AliceId}/books/return", returnReq);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkout = await response.Content.ReadFromJsonAsync<CheckoutDTO>();
        checkout!.ReturnedAt.Should().NotBeNull();

        // Book should now show as available
        var book = await _client.GetFromJsonAsync<BookDTO>("/books/978-0-7432-7356-5");
        book!.Status.Should().Be("available");
    }

    [Fact]
    public async Task ReturnBook_NoActiveCheckout_Returns404()
    {
        // Bob hasn't checked out anything
        var returnReq = new HireBookRequest("978-0-06-112008-4");
        var response = await _client.PostAsJsonAsync($"/members/{BobId}/books/return", returnReq);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
