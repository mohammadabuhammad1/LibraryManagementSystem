namespace LibraryManagement.Application.Dtos.Book
{
    public class CreateBorrowRecordDto
    {
        public int BookId { get; set; }
        public string UserId { get; set; }
        public int BorrowDurationDays { get; set; } = 14;
        public string? Notes { get; set; }
    }

}
