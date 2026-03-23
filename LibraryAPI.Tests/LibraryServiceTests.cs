using FluentAssertions;
using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.ModelsDTO;
using LibraryAPI.Services;
using Xunit;

namespace LibraryApi.Tests;

public class LibraryServiceTests
{
    private static LibraryService CreateService() => new(new MockData());

    private static readonly Guid AliceId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid BobId   = Guid.Parse("22222222-0000-0000-0000-000000000002");

    [Fact]
    public void GetAllBooks_ReturnsSeedBooks()
    {
        var svc = CreateService();
        var books = svc.GetAllBooks().ToList();
        books.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void GetAllBooks_CheckedOutBook_HasCorrectStatus()
    {
        var svc = CreateService();
        var checkedOut = svc.GetAllBooks().Where(b => b.Status == "checked out").ToList();
        checkedOut.Should().ContainSingle(b => b.ISBN == "978-0-7432-7356-5"); // 1984
    }

    [Fact]
    public void HireBook_Available_Succeeds()
    {
        var svc = CreateService();
        var (result, error) = svc.HireBook(BobId, "978-0-06-112008-4");

        error.Should().BeNull();
        result.Should().NotBeNull();
        result!.MemberId.Should().Be(BobId);

        // Book status should flip
        svc.GetBook("978-0-06-112008-4")!.Status.Should().Be("checked out");
    }

    [Fact]
    public void HireBook_AlreadyCheckedOut_ReturnsError()
    {
        var svc = CreateService();
        var (result, error) = svc.HireBook(BobId, "978-0-7432-7356-5"); // Alice already has it

        result.Should().BeNull();
        error.Should().Contain("already checked out");
    }

    [Fact]
    public void HireBook_UnknownMember_ReturnsError()
    {
        var svc = CreateService();
        var (result, error) = svc.HireBook(Guid.NewGuid(), "978-0-06-112008-4");

        result.Should().BeNull();
        error.Should().Contain("Member not found");
    }

    [Fact]
    public void HireBook_UnknownISBN_ReturnsError()
    {
        var svc = CreateService();
        var (result, error) = svc.HireBook(BobId, "000-none");

        result.Should().BeNull();
        error.Should().Contain("Book not found");
    }

    [Fact]
    public void ReturnBook_ActiveCheckout_SetsReturnedAt()
    {
        var svc = CreateService();
        var (result, error) = svc.ReturnBook(AliceId, "978-0-7432-7356-5");

        error.Should().BeNull();
        result!.ReturnedAt.Should().NotBeNull();
        svc.GetBook("978-0-7432-7356-5")!.Status.Should().Be("available");
    }

    [Fact]
    public void ReturnBook_NoActiveCheckout_ReturnsError()
    {
        var svc = CreateService();
        var (result, error) = svc.ReturnBook(BobId, "978-0-06-112008-4");

        result.Should().BeNull();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetCheckedOutByMember_OnlyReturnsActiveCheckouts()
    {
        var svc = CreateService();
        // Return Alice's book first
        svc.ReturnBook(AliceId, "978-0-7432-7356-5");

        var active = svc.GetCheckedOutByMember(AliceId).ToList();
        active.Should().BeEmpty();
    }

    [Fact]
    public void AddBook_ThenRetrieve_Works()
    {
        var svc = CreateService();
        svc.AddBook(new CreateBookRequest("111-0-00-000000-1", "New Book", "New Author", 2025, "Sci-Fi"));

        var book = svc.GetBook("111-0-00-000000-1");
        book.Should().NotBeNull();
        book!.Status.Should().Be("available");
    }
}
