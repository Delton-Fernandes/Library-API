using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.ModelsDTO;

// ---------- Books ----------

public record BookDTO(
    string ISBN,
    string Title,
    string Author,
    int PublishedYear,
    string Genre,
    string Status          // "available" | "checked out"
);

public record CreateBookRequest(
    [Required, StringLength(20, MinimumLength = 10)] string ISBN,
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(200, MinimumLength = 1)] string Author,
    [Range(1000, 2100)] int PublishedYear,
    [Required, StringLength(100, MinimumLength = 1)] string Genre
);

// ---------- Members ----------

public record MemberDTO(
    Guid Id,
    string FullName,
    string Email,
    DateTime MemberSince
);

public record CreateMemberRequest(
    [Required, StringLength(200, MinimumLength = 1)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email
);

// ---------- Checkouts ----------

public record CheckoutDTO(
    Guid Id,
    Guid MemberId,
    string BookISBN,
    string BookTitle,
    DateTime CheckedOutAt,
    DateTime? ReturnedAt
);

public record HireBookRequest(
    [Required, StringLength(20, MinimumLength = 10)] string BookISBN
);

// ---------- Generic responses ----------

public record ErrorResponse(string Message);