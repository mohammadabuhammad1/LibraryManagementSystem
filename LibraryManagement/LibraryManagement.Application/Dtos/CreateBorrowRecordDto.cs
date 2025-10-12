namespace LibraryManagement.Application.Dtos
{
    public class CreateBorrowRecordDto
    {
        public int BookId { get; set; }
        public int MemberId { get; set; }
        public int BorrowDurationDays { get; set; } = 14;
        public string? Notes { get; set; }
    }

}
