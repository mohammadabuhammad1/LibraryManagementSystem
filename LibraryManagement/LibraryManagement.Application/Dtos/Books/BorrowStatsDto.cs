
namespace LibraryManagement.Application.Dtos.Books;
public class BorrowStatsDto
{
    public int TotalOverdue { get; set; }
    public decimal TotalFines { get; set; }
    public string MostBorrowedBooks { get; set; } = string.Empty;
}