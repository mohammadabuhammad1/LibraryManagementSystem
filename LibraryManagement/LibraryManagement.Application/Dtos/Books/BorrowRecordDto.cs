namespace LibraryManagement.Application.Dtos.Books
{
    public class BorrowRecordDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string UserId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public bool IsOverdue => !IsReturned && DueDate < DateTime.UtcNow;
        public decimal? FineAmount { get; set; }
        public string? Notes { get; set; }
    }

}
