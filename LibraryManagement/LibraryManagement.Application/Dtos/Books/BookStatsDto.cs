namespace LibraryManagement.Application.Dtos.Books;
public class BookStatsDto
{
    public int TotalBooks { get; set; }
    public int AvailableBooks { get; set; }
    public int BorrowedBooks { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
