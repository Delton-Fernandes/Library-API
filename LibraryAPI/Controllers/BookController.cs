using LibraryAPI.Models;
using LibraryAPI.Services;
using LibraryAPI.ModelsDTO;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Controllers;

/// <summary>Manage the library book catalogue.</summary>
[ApiController]
[Route("books")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly ILibraryService _library;
    public BooksController(ILibraryService library) => _library = library;

    /// <summary>List all books with their current availability status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookDTO>), StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_library.GetAllBooks());

    /// <summary>Get a single book by ISBN.</summary>
    [HttpGet("{isbn}")]
    [ProducesResponseType(typeof(BookDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetById(string isbn)
    {
        var book = _library.GetBook(isbn);
        return book is null
            ? NotFound(new ErrorResponse($"Book '{isbn}' not found."))
            : Ok(book);
    }

    /// <summary>Add a new book to the library catalogue.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public IActionResult Create([FromBody] CreateBookRequest request)
    {
        if (_library.GetBook(request.ISBN) is not null)
            return Conflict(new ErrorResponse($"A book with ISBN '{request.ISBN}' already exists."));

        var book = _library.AddBook(request);
        return CreatedAtAction(nameof(GetById), new { isbn = book.ISBN }, book);
    }
}