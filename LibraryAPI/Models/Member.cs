namespace LibraryAPI.Models;

public class Member
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
}