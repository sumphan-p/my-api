namespace AuthAPI.Models;

// === Standard API Response ===

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponse<T> Success(T data, string message = "Success")
        => new() { Data = data, Message = message };
}

public class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<FieldError>? Errors { get; set; }

    public static ApiErrorResponse Create(string code, string message, List<FieldError>? errors = null)
        => new() { Code = code, Message = message, Errors = errors };
}

public class FieldError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
