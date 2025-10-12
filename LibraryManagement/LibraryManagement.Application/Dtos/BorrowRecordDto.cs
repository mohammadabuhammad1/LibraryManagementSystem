namespace LibraryManagement.Application.Dtos
{
    public class BorrowRecordDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int MemberId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public bool IsOverdue => !IsReturned && DueDate < DateTime.UtcNow;
        public decimal? FineAmount { get; set; }
        public string? Notes { get; set; }
    }

}
