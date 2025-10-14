namespace LibraryManagement.Application.Dtos.Libraries
{
    public class LibraryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BookCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
