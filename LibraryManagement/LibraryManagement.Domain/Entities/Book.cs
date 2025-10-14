namespace LibraryManagement.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int TotalCopies { get; set; }
    public int CopiesAvailable { get; set; }
    public bool IsAvailable => CopiesAvailable > 0;

    public int? LibraryId { get; set; }
    public virtual Library? Library { get; set; }
    
    // Make sure this navigation property exists
    public virtual ICollection<BorrowRecord> BorrowRecords { get; } = [];

}