using LibraryAPI.ModelsDTO;
using LibraryAPI.Models;
using LibraryAPI.Data;


namespace LibraryAPI.Services;

public class LibraryService : ILibraryService
{
    private readonly MockData _store;

    public LibraryService(MockData store) => _store = store;

    // ── Books ──────────────────────────────────────────────────────────────────

    public IEnumerable<BookDTO> GetAllBooks()
    {
        var activeISBNs = _store.Checkouts
            .Where(c => c.IsActive)
            .Select(c => c.BookISBN)
            .ToHashSet();

        return _store.Books.Select(b => MapBook(b, activeISBNs.Contains(b.ISBN)));
    }

    public BookDTO? GetBook(string isbn)
    {
        var book = _store.Books.FirstOrDefault(b => b.ISBN == isbn);
        if (book is null) return null;

        bool checkedOut = _store.Checkouts.Any(c => c.BookISBN == isbn && c.IsActive);
        return MapBook(book, checkedOut);
    }

    public BookDTO AddBook(CreateBookRequest req)
    {
        var book = new Book
        {
            ISBN = req.ISBN,
            Title = req.Title,
            Author = req.Author,
            PublishedYear = req.PublishedYear,
            Genre = req.Genre,
        };
        _store.Books.Add(book);
        return MapBook(book, false);
    }

    // ── Members ────────────────────────────────────────────────────────────────

    public IEnumerable<MemberDTO> GetAllMembers() =>
        _store.Members.Select(MapMember);

    public MemberDTO? GetMember(Guid id) =>
        _store.Members.FirstOrDefault(m => m.Id == id) is { } m ? MapMember(m) : null;

    public MemberDTO AddMember(CreateMemberRequest req)
    {
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FullName = req.FullName,
            Email = req.Email,
            MemberSince = DateTime.UtcNow,
        };
        _store.Members.Add(member);
        return MapMember(member);
    }

    // ── Checkouts ──────────────────────────────────────────────────────────────

    public IEnumerable<CheckoutDTO> GetCheckedOutByMember(Guid memberId) =>
        _store.Checkouts
            .Where(c => c.MemberId == memberId && c.IsActive)
            .Select(c => MapCheckout(c));

    public (CheckoutDTO? Result, string? Error) HireBook(Guid memberId, string isbn)
    {
        if (_store.Members.All(m => m.Id != memberId))
            return (null, "Member not found.");

        if (_store.Books.All(b => b.ISBN != isbn))
            return (null, "Book not found.");

        if (_store.Checkouts.Any(c => c.BookISBN == isbn && c.IsActive))
            return (null, "Book is already checked out.");

        var checkout = new Checkout
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            BookISBN = isbn,
            CheckedOutAt = DateTime.UtcNow,
        };
        _store.Checkouts.Add(checkout);
        return (MapCheckout(checkout), null);
    }

    public (CheckoutDTO? Result, string? Error) ReturnBook(Guid memberId, string isbn)
    {
        var checkout = _store.Checkouts
            .FirstOrDefault(c => c.MemberId == memberId && c.BookISBN == isbn && c.IsActive);

        if (checkout is null)
            return (null, "No active checkout found for this member and book.");

        checkout.ReturnedAt = DateTime.UtcNow;
        return (MapCheckout(checkout), null);
    }

    // ── Mappers ────────────────────────────────────────────────────────────────

    private static BookDTO MapBook(Book b, bool checkedOut) =>
        new(b.ISBN, b.Title, b.Author, b.PublishedYear, b.Genre,
            checkedOut ? "checked out" : "available");

    private static MemberDTO MapMember(Member m) =>
        new(m.Id, m.FullName, m.Email, m.MemberSince);

    private CheckoutDTO MapCheckout(Checkout c)
    {
        var title = _store.Books.FirstOrDefault(b => b.ISBN == c.BookISBN)?.Title ?? c.BookISBN;
        return new CheckoutDTO(c.Id, c.MemberId, c.BookISBN, title, c.CheckedOutAt, c.ReturnedAt);
    }
}