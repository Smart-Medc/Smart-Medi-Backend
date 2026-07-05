namespace Smart_Medc.Application.Common
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ServiceResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
        public static ServiceResult<T> Failure(string error, int statusCode = 400) =>
            new() { IsSuccess = false, ErrorMessage = error, StatusCode = statusCode };
        public static ServiceResult<T> NotFound(string error = "Resource not found") =>
            new() { IsSuccess = false, ErrorMessage = error, StatusCode = 404 };
    }
    
    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ServiceResult Success() => new() { IsSuccess = true };
        public static ServiceResult Failure(string error, int statusCode = 400) =>
            new() { IsSuccess = false, ErrorMessage = error, StatusCode = statusCode };
        public static ServiceResult NotFound(string error = "Resource not found") =>
            new() { IsSuccess = false, ErrorMessage = error, StatusCode = 404 };
    }
}
