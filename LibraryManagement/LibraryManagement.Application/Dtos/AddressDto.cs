using System.ComponentModel.DataAnnotations;

namespace API.Dtos;

public class AddressDto
{
    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    public required string Street { get; set; }

    [Required]
    public required string City { get; set; }

    [Required]
    public required string State { get; set; }

    [Required]
    public required string Zipcode { get; set; }
}