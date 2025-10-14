namespace LibraryManagement.Domain.Entities;


public class Library : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<Book> Books { get;  } = [];
}
