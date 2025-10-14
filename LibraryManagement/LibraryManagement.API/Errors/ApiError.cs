namespace LibraryManagement.API.Errors;

internal sealed class ApiError : ApiResponse
{
    public ApiError(int statusCode, string? message = null, string? details = null)
        : base(statusCode, message)
    {
        Details = details;
    }

    public string? Details { get; set; }
}

/*
* after this class, we'll create some middleware so that we can handle exceptions and use ApiError Class,
* In event that we get an exception
*/