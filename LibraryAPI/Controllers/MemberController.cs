using LibraryAPI.Models;
using LibraryAPI.Services;
using LibraryAPI.ModelsDTO;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Controllers;

/// <summary>Manage library members.</summary>
[ApiController]
[Route("members")]
[Produces("application/json")]
public class MembersController : ControllerBase
{
    private readonly ILibraryService _library;
    public MembersController(ILibraryService library) => _library = library;

    /// <summary>List all members.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MemberDTO>), StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_library.GetAllMembers());

    /// <summary>Get a single member by ID.</summary>
    [HttpGet("{memberId:guid}")]
    [ProducesResponseType(typeof(MemberDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid memberId)
    {
        var member = _library.GetMember(memberId);
        return member is null
            ? NotFound(new ErrorResponse($"Member '{memberId}' not found."))
            : Ok(member);
    }

    /// <summary>Register a new library member.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MemberDTO), StatusCodes.Status201Created)]
    public IActionResult Create([FromBody] CreateMemberRequest request)
    {
        var member = _library.AddMember(request);
        return CreatedAtAction(nameof(GetById), new { memberId = member.Id }, member);
    }

    /// <summary>List all books currently checked out by a member.</summary>
    [HttpGet("{memberId:guid}/books/checkedout")]
    [ProducesResponseType(typeof(IEnumerable<CheckoutDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetCheckedOut(Guid memberId)
    {
        if (_library.GetMember(memberId) is null)
            return NotFound(new ErrorResponse($"Member '{memberId}' not found."));

        return Ok(_library.GetCheckedOutByMember(memberId));
    }

    /// <summary>Check out (hire) a book for a member.</summary>
    [HttpPost("{memberId:guid}/books/hire")]
    [ProducesResponseType(typeof(CheckoutDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public IActionResult HireBook(Guid memberId, [FromBody] HireBookRequest request)
    {
        var (result, error) = _library.HireBook(memberId, request.BookISBN);

        if (error == "Member not found.") return NotFound(new ErrorResponse(error));
        if (error == "Book not found.") return NotFound(new ErrorResponse(error));
        if (error != null) return Conflict(new ErrorResponse(error));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Return a checked-out book.</summary>
    [HttpPost("{memberId:guid}/books/return")]
    [ProducesResponseType(typeof(CheckoutDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult ReturnBook(Guid memberId, [FromBody] HireBookRequest request)
    {
        var (result, error) = _library.ReturnBook(memberId, request.BookISBN);

        return error is not null
            ? NotFound(new ErrorResponse(error))
            : Ok(result);
    }
}