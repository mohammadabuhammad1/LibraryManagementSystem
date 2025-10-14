namespace LibraryManagement.Application.Dtos.Books;

public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int TotalCopies { get; set; }
    public int CopiesAvailable { get; set; }
    public int LibraryId { get; set; }
}
