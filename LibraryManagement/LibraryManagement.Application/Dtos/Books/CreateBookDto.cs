namespace LibraryManagement.Application.Dtos.Books;

public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int TotalCopies { get; set; }
}
