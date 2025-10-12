namespace LibraryManagement.API.Errors
{
    public class ApiException : ApiResponse
    {
        public ApiException(int statusCode, string message = null ,
            string details = null) : base(statusCode, message)
        {
            Details = details;
        }
        public string Details { get; set; }
    }
}
/*
 * after this class, we'll create some middleware so that we can handle exceptions and use ApiException Class,
 * In event that we get an exception
 */