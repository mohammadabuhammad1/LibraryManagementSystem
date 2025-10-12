namespace LibraryManagement.Application.Dtos
{
    public class ReturnBookDto
    {
        public int BookId { get; set; }
        public int MemberId { get; set; }
        public string? Notes { get; set; }
    }

}
