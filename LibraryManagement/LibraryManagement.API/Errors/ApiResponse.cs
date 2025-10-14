namespace LibraryManagement.API.Errors;

internal class ApiResponse
{
    public ApiResponse()
    {
    }

    public ApiResponse(int statusCode, string? message = null)
    {
        StatusCode = statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
    }

    public int StatusCode { get; set; }
    public string? Message { get; set; }

    private static string GetDefaultMessageForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "The request contains invalid parameters or malformed syntax.",
            401 => "Authentication is required to access this resource.",
            403 => "You do not have permission to access this resource.",
            404 => "The requested resource was not found.",
            409 => "The request conflicts with the current state of the resource.",
            422 => "The request was well-formed but contains semantic validation errors.",
            500 => "An unexpected error occurred while processing your request. Please try again later.",
            503 => "The service is temporarily unavailable. Please try again later.",
            _ => "An error occurred while processing your request."
        };
    }
}