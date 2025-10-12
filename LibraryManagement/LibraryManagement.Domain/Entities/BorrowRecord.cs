namespace LibraryManagement.Domain.Entities
{
    public class BorrowRecord : BaseEntity
    {
        public int BookId { get; set; }
        public string UserId { get; set; } = string.Empty; // Changed to string
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public decimal? FineAmount { get; set; }
        public string? Notes { get; set; }

        // Navigation properties - UPDATED
        public Book Book { get; set; } 
        public ApplicationUser User { get; set; } 
    }
}