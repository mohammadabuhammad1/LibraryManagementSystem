namespace LibraryManagement.Application.Dtos.Book;

public class BookSearchDto
{
    public string? Query { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public int? PublishedYear { get; set; }
    public string? ISBN { get; set; }
    public bool? AvailableOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
