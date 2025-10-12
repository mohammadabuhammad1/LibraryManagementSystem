namespace LibraryManagement.Domain.Entities
{
    public class BorrowRecord : BaseEntity
        {
            public int BookId { get; set; }
            public int MemberId { get; set; }
            public DateTime BorrowDate { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime? ReturnDate { get; set; }
            public bool IsReturned { get; set; }
            public decimal? FineAmount { get; set; }
            public string? Notes { get; set; }

            // Navigation properties
            public virtual Book Book { get; set; } = null!;
            public virtual Member Member { get; set; } = null!;
        
    }
}