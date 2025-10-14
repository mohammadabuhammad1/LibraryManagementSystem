namespace LibraryManagement.API.Errors;

internal sealed class ApiValidationErrorResponse : ApiResponse
{
    public ApiValidationErrorResponse() : base(400)
    {
        Errors = new List<string>();
    }

    public IEnumerable<string> Errors { get; set; }
}