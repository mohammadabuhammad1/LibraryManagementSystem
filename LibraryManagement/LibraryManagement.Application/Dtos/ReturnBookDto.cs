namespace LibraryManagement.Application.Dtos
{
    public class ReturnBookDto
    {
        public int BookId { get; set; }
        public string UserId { get; set; }
        public string? Notes { get; set; }
    }

}
