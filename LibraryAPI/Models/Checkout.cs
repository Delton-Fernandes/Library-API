namespace LibraryAPI.Models;

public class Checkout
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string BookISBN { get; set; } = string.Empty;
    public DateTime CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    /// <summary>True while ReturnedAt is null</summary>
    public bool IsActive => ReturnedAt is null;
}
