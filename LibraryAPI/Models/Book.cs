namespace LibraryAPI.Models;

public class Book
{
    /// <summary>ISBN identifier for the book</summary>
    public string ISBN { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public string Genre { get; set; } = string.Empty;

    /// <summary>Derived from active checkouts</summary>
    public string Status { get; set; } = "available"; // "available" | "checked out"
}