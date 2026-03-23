using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.ModelsDTO;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Controllers;

/// <summary>
/// Admin view of all checkout history.
/// Useful for reporting — not scoped to a specific member.
/// </summary>
[ApiController]
[Route("checkouts")]
[Produces("application/json")]
public class CheckoutsController : ControllerBase
{
    private readonly MockData _store;

    public CheckoutsController(MockData store) => _store = store;

    /// <summary>List all checkouts (active and returned), optionally filtered.</summary>
    /// <param name="activeOnly">When true, only return checkouts that have not been returned.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CheckoutSummaryDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll([FromQuery] bool activeOnly = false)
    {
        var query = _store.Checkouts.AsEnumerable();

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        var result = query
            .OrderByDescending(c => c.CheckedOutAt)
            .Select(c => ToDto(c))
            .ToList();

        return Ok(result);
    }

    /// <summary>Get a single checkout record by its ID.</summary>
    [HttpGet("{checkoutId:guid}")]
    [ProducesResponseType(typeof(CheckoutSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid checkoutId)
    {
        var checkout = _store.Checkouts.FirstOrDefault(c => c.Id == checkoutId);
        return checkout is null
            ? NotFound(new ErrorResponse($"Checkout '{checkoutId}' not found."))
            : Ok(ToDto(checkout));
    }

    // ── Mapper ─────────────────────────────────────────────────────────────────

    private CheckoutSummaryDto ToDto(Checkout c)
    {
        var book = _store.Books.FirstOrDefault(b => b.ISBN == c.BookISBN);
        var member = _store.Members.FirstOrDefault(m => m.Id == c.MemberId);

        return new CheckoutSummaryDto(
            c.Id,
            c.MemberId,
            member?.FullName ?? "Unknown",
            c.BookISBN,
            book?.Title ?? c.BookISBN,
            c.CheckedOutAt,
            c.ReturnedAt,
            c.IsActive ? "active" : "returned"
        );
    }
}

/// <summary>Flattened checkout record including member and book names.</summary>
public record CheckoutSummaryDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string BookISBN,
    string BookTitle,
    DateTime CheckedOutAt,
    DateTime? ReturnedAt,
    string Status        // "active" | "returned"
);