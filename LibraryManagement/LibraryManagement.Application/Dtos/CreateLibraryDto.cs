namespace LibraryManagement.Application.Dtos
{
    public class CreateLibraryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
