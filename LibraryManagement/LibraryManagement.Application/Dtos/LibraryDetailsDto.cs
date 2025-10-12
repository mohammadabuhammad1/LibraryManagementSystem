namespace LibraryManagement.Application.Dtos
{
    public class LibraryDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IEnumerable<BookDto> Books { get; set; } = new List<BookDto>();
        public DateTime CreatedAt { get; set; }
    }
}
