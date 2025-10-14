using LibraryManagement.Application.Dtos.Books;

namespace LibraryManagement.Application.Dtos.Libraries;

public class LibraryDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IEnumerable<BookDto> Books { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
