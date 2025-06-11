namespace stibe.api.Models.DTOs.Features
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }

        public static ApiResponse<T> ErrorResponse(List<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            };
        }
    }

    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse SuccessResponse(string message = "Success")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public static new ApiResponse ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}