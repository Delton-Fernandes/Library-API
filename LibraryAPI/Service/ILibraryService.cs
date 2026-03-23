using LibraryAPI.ModelsDTO;
public interface ILibraryService
{
    // Books
    IEnumerable<BookDTO> GetAllBooks();
    BookDTO? GetBook(string isbn);
    BookDTO AddBook(CreateBookRequest request);

    // Members
    IEnumerable<MemberDTO> GetAllMembers();
    MemberDTO? GetMember(Guid memberId);
    MemberDTO AddMember(CreateMemberRequest request);

    // Checkouts
    IEnumerable<CheckoutDTO> GetCheckedOutByMember(Guid memberId);
    (CheckoutDTO? Result, string? Error) HireBook(Guid memberId, string isbn);
    (CheckoutDTO? Result, string? Error) ReturnBook(Guid memberId, string isbn);
}