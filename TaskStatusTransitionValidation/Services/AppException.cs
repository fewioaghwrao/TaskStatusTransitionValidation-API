// ============================
// Services/AppException.cs
// ============================
namespace TaskStatusTransitionValidation.Services;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string? Title { get; }
    public string? Detail { get; }
    public string? Type { get; }
    public Dictionary<string, object>? Extensions { get; }

    public AppException(
        int statusCode,
        string title,
        string? detail = null,
        string? type = null,
        Dictionary<string, object>? extensions = null
    ) : base(detail ?? title)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Type = type;
        Extensions = extensions;
    }

    public static AppException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "Not Found", detail);

    public static AppException Forbidden(string detail) =>
        new(StatusCodes.Status403Forbidden, "Forbidden", detail);

    public static AppException Unauthorized(string detail) =>
        new(StatusCodes.Status401Unauthorized, "Unauthorized", detail);

    public static AppException BadRequest(string detail, Dictionary<string, object>? ext = null) =>
        new(StatusCodes.Status400BadRequest, "Bad Request", detail, extensions: ext);

    public static AppException Conflict(string detail, Dictionary<string, object>? ext = null) =>
        new(StatusCodes.Status409Conflict, "Conflict", detail, extensions: ext);
}

