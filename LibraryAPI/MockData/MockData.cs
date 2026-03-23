using LibraryAPI.Models;

namespace LibraryAPI.Data;

/// <summary>
/// Simple in-memory store.  All state is lost on restart – intentional for this exercise.
/// </summary>
public class MockData
{
    public List<Book> Books { get; } = new()
    {
        new Book { ISBN = "978-0-06-112008-4",  Title = "To Kill a Mockingbird", Author = "Harper Lee",        PublishedYear = 1960, Genre = "Fiction" },
        new Book { ISBN = "978-0-7432-7356-5",  Title = "1984",                  Author = "George Orwell",     PublishedYear = 1949, Genre = "Dystopian" },
        new Book { ISBN = "978-0-7432-7357-2",  Title = "The Great Gatsby",      Author = "F. Scott Fitzgerald",PublishedYear = 1925, Genre = "Classic" },
        new Book { ISBN = "978-0-14-028329-7",  Title = "Of Mice and Men",       Author = "John Steinbeck",    PublishedYear = 1937, Genre = "Fiction" },
        new Book { ISBN = "978-0-06-093546-9",  Title = "To the Lighthouse",     Author = "Virginia Woolf",    PublishedYear = 1927, Genre = "Modernist" },
    };

    public List<Member> Members { get; } = new()
    {
        new Member { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), FullName = "Alice Johnson", Email = "alice@example.com", MemberSince = new DateTime(2022, 3, 15) },
        new Member { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), FullName = "Bob Smith",    Email = "bob@example.com",   MemberSince = new DateTime(2023, 7, 1)  },
        new Member { Id = Guid.Parse("33333333-0000-0000-0000-000000000003"), FullName = "Carol White",  Email = "carol@example.com", MemberSince = new DateTime(2024, 1, 20) },
    };

    public List<Checkout> Checkouts { get; } = new()
    {
        // Alice has checked out 1984
        new Checkout
        {
            Id           = Guid.NewGuid(),
            MemberId     = Guid.Parse("11111111-0000-0000-0000-000000000001"),
            BookISBN     = "978-0-7432-7356-5",
            CheckedOutAt = DateTime.UtcNow.AddDays(-5)
        }
    };
}