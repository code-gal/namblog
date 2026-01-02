namespace NamBlog.API.Application.Common
{
    /// <summary>
    /// 统一结果类型（Result Pattern）
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public string? ErrorCode { get; }

        protected Result(bool isSuccess, string? errorMessage = null, string? errorCode = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        public static Result Success() => new(true);
        public static Result Failure(string errorMessage, string? errorCode = null)
            => new(false, errorMessage, errorCode);

        public static Result<T> Success<T>(T value) => new(value, true);
        public static Result<T> Failure<T>(string errorMessage, string? errorCode = null)
            => new(default, false, errorMessage, errorCode);
    }

    /// <summary>
    /// 泛型结果类型（带返回值）
    /// </summary>
    public class Result<T> : Result
    {
        public T? Value { get; }

        internal Result(T? value, bool isSuccess, string? errorMessage = null, string? errorCode = null)
            : base(isSuccess, errorMessage, errorCode)
        {
            Value = value;
        }
    }

    /// <summary>
    /// 常用错误代码
    /// </summary>
    public static class ErrorCodes
    {
        public const string NotFound = "NOT_FOUND";
        public const string AlreadyExists = "ALREADY_EXISTS";
        public const string InvalidOperation = "INVALID_OPERATION";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
        public const string InternalError = "INTERNAL_ERROR";
    }
}
