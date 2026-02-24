namespace Smart_Medc.Application.Common.Results
{
    public class Result
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();

        public static Result Success(string message = "Operation successful")
            => new() { IsSuccess = true, Message = message };

        public static Result Failure(string message, params string[] errors)
            => new() { IsSuccess = false, Message = message, Errors = errors.ToList() };

        public static Result Failure(List<string> errors)
            => new() { IsSuccess = false, Message = "Operation failed", Errors = errors };
    }

    public class Result<T> : Result
    {
        public T? Data { get; set; }

        public static Result<T> Success(T data, string message = "Operation successful")
            => new() { IsSuccess = true, Data = data, Message = message };

        public new static Result<T> Failure(string message, params string[] errors)
            => new() { IsSuccess = false, Message = message, Errors = errors.ToList() };

        public new static Result<T> Failure(List<string> errors)
            => new() { IsSuccess = false, Message = "Operation failed", Errors = errors };
    }
}